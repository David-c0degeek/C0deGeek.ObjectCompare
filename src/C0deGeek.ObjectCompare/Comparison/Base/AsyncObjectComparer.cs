using C0deGeek.ObjectCompare.Interfaces;
using C0deGeek.ObjectCompare.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace C0deGeek.ObjectCompare.Comparison.Base;

/// <summary>
/// Provides asynchronous comparison capabilities for large object graphs
/// </summary>
public class AsyncObjectComparer : IDisposable
{
    private readonly ObjectComparer _baseComparer;
    private readonly ComparisonConfig _config;
    private readonly ILogger _logger;
    private readonly int _batchSize;
    private readonly SemaphoreSlim _throttle;
    private bool _disposed;

    public AsyncObjectComparer(ComparisonConfig? config = null, int batchSize = 100, int maxConcurrency = -1)
    {
        _config = config ?? new ComparisonConfig();
        _baseComparer = new ObjectComparer(_config);
        _logger = _config.Logger ?? NullLogger.Instance;
        _batchSize = batchSize;
        _throttle = new SemaphoreSlim(
            maxConcurrency > 0 ? maxConcurrency : Environment.ProcessorCount * 2);
    }

    public async Task<ComparisonResult> CompareAsync<T>(T? obj1, T? obj2, 
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _logger.LogDebug("Starting async comparison of {Type}", typeof(T).Name);

        var result = new ComparisonResult();
        var context = new ComparisonContext();

        try
        {
            context.Timer.Start();
            await CompareObjectsAsync(obj1, obj2, "", result, context, cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Comparison cancelled for {Type}", typeof(T).Name);
            result.Differences.Add("Comparison was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Async comparison failed for {Type}", typeof(T).Name);
            throw;
        }
        finally
        {
            context.Timer.Stop();
            UpdateResultMetrics(result, context);
            LogComparisonMetrics(result);
        }
    }

    private async Task CompareObjectsAsync(object? obj1, object? obj2, string path,
        ComparisonResult result, ComparisonContext context, CancellationToken cancellationToken)
    {
        var workItems = new Queue<ComparisonWorkItem>();
        workItems.Enqueue(new ComparisonWorkItem(obj1, obj2, path, 0));

        while (workItems.Count > 0)
        {
            var batch = new List<ComparisonWorkItem>();
            for (var i = 0; i < _batchSize && workItems.Count > 0; i++)
            {
                batch.Add(workItems.Dequeue());
            }

            var tasks = batch.Select(item => 
                ProcessWorkItemAsync(item, workItems, result, context, cancellationToken));

            await Task.WhenAll(tasks);

            if (workItems.Count > 0)
            {
                await Task.Yield();
            }
        }
    }

    private async Task ProcessWorkItemAsync(ComparisonWorkItem item,
        Queue<ComparisonWorkItem> workItems, ComparisonResult result,
        ComparisonContext context, CancellationToken cancellationToken)
    {
        await _throttle.WaitAsync(cancellationToken);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item.Obj1 is IAsyncComparable<object> asyncComparable)
            {
                var asyncResult = await asyncComparable.CompareToAsync(item.Obj2, cancellationToken);
                if (!asyncResult)
                {
                    result.Differences.Add($"Async comparison failed at {item.Path}");
                    result.AreEqual = false;
                }
                return;
            }

            // For collections, process items in parallel
            if (item.Obj1 is IEnumerable enumerable1 && item.Obj2 is IEnumerable enumerable2)
            {
                await CompareCollectionsAsync(enumerable1, enumerable2, item.Path,
                    result, workItems, item.Depth, cancellationToken);
                return;
            }

            // For regular objects, use the base comparer
            var tempResult = _baseComparer.Compare(item.Obj1, item.Obj2);
            if (!tempResult.AreEqual)
            {
                lock (result)
                {
                    result.Differences.AddRange(tempResult.Differences);
                    result.AreEqual = false;
                }
            }
        }
        finally
        {
            _throttle.Release();
        }
    }

    public async Task CompareCollectionsAsync(IEnumerable collection1,
        IEnumerable collection2, string path, 
        ComparisonResult result,
        Queue<ComparisonWorkItem> workItems, int depth, 
        CancellationToken cancellationToken)
    {
        var list1 = collection1.Cast<object>().ToList();
        var list2 = collection2.Cast<object>().ToList();

        if (list1.Count != list2.Count)
        {
            lock (result)
            {
                result.Differences.Add($"Collection length mismatch at {path}");
                result.AreEqual = false;
            }
            return;
        }

        for (var i = 0; i < list1.Count; i++)
        {
            workItems.Enqueue(new ComparisonWorkItem(
                list1[i], list2[i], $"{path}[{i}]", depth + 1));

            if (i % _batchSize == 0)
            {
                await Task.Yield();
            }
        }
    }

    private void UpdateResultMetrics(ComparisonResult result, ComparisonContext context)
    {
        result.ComparisonTime = context.Timer.Elapsed;
        result.ObjectsCompared = context.ObjectsCompared;
        result.MaxDepthReached = context.MaxDepthReached;
    }

    private void LogComparisonMetrics(ComparisonResult result)
    {
        _logger.LogInformation(
            "Async comparison completed in {Time}ms. Objects compared: {Objects}, Max depth: {Depth}, Differences: {Differences}",
            result.ComparisonTime.TotalMilliseconds,
            result.ObjectsCompared,
            result.MaxDepthReached,
            result.Differences.Count);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AsyncObjectComparer));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _throttle.Dispose();
        _baseComparer.Dispose();
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }
}