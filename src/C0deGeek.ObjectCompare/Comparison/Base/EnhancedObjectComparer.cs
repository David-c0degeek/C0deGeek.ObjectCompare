using System.Collections.Concurrent;
using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Interfaces;
using C0deGeek.ObjectCompare.Performance;
using C0deGeek.ObjectCompare.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace C0deGeek.ObjectCompare.Comparison.Base;

/// <summary>
/// Enhanced object comparer with advanced features and resource management
/// </summary>
public sealed class EnhancedObjectComparer : DisposableComparisonComponent
{
    private readonly ComparisonConfig _config;
    private readonly ComparisonResourcePool _resourcePool;
    private readonly ConcurrentDictionary<string, IDisposable> _activeComparisons;
    private readonly PerformanceMonitor _performanceMonitor;

    public EnhancedObjectComparer(ComparisonConfig config) : base(config.Logger ?? NullLogger.Instance)
    {
        _config = Guard.ThrowIfNull(config, nameof(config));
        _resourcePool = new ComparisonResourcePool();
        _activeComparisons = new ConcurrentDictionary<string, IDisposable>();
        _performanceMonitor = new PerformanceMonitor(Logger);
    }

    public async Task<ComparisonResult> CompareAsync<T>(T? obj1, T? obj2,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var comparisonId = Guid.NewGuid().ToString();
        var result = new ComparisonResult();

        using var operation = _performanceMonitor.TrackOperation($"Compare_{typeof(T).Name}");
        
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, CancellationToken);
        using var timeoutCts = new CancellationTokenSource(_config.ComparisonTimeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            linkedCts.Token, timeoutCts.Token);

        try
        {
            var comparer = await _resourcePool.AcquireComparerAsync<ObjectComparer>();
            _activeComparisons.TryAdd(comparisonId,
            comparer);

            var asyncComparer = new AsyncObjectComparer(_config);
            result = await asyncComparer.CompareAsync(obj1, obj2, combinedCts.Token);

            // Track performance metrics
            _performanceMonitor.IncrementObjectCount(typeof(T).Name);
            
            return result;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            var timeoutMessage = ExceptionHelper.CreateTimeoutMessage(
                "Comparison", _config.ComparisonTimeout);
            Logger.LogWarning(timeoutMessage);
            result.Differences.Add(timeoutMessage);
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Comparison failed for type {Type}", typeof(T).Name);
            throw;
        }
        finally
        {
            if (_activeComparisons.TryRemove(comparisonId, out var comparison))
            {
                _resourcePool.ReleaseComparer(comparison);
            }
        }
    }

    public PerformanceReport GetPerformanceReport()
    {
        ThrowIfDisposed();
        return _performanceMonitor.GenerateReport();
    }

    public void CancelActiveComparisons()
    {
        foreach (var comparison in _activeComparisons)
        {
            try
            {
                if (_activeComparisons.TryRemove(comparison.Key, out var comp))
                {
                    _resourcePool.ReleaseComparer(comp);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error cancelling comparison {Id}", comparison.Key);
            }
        }
    }

    protected override void DisposeResources()
    {
        CancelActiveComparisons();
        _resourcePool.Dispose();
    }

    public class Builder
    {
        private readonly ComparisonConfig _config = new();

        public Builder WithTimeout(TimeSpan timeout)
        {
            _config.ComparisonTimeout = timeout;
            return this;
        }

        public Builder WithMaxDepth(int maxDepth)
        {
            _config.MaxDepth = maxDepth;
            return this;
        }

        public Builder WithMaxObjectCount(int maxObjectCount)
        {
            _config.MaxObjectCount = maxObjectCount;
            return this;
        }

        public Builder WithLogger(ILogger logger)
        {
            _config.Logger = logger;
            return this;
        }

        public Builder IgnoreCollectionOrder(bool ignore = true)
        {
            _config.IgnoreCollectionOrder = ignore;
            return this;
        }

        public Builder WithCustomComparer<T>(ICustomComparer comparer)
        {
            _config.CustomComparers[typeof(T)] = comparer;
            return this;
        }

        public EnhancedObjectComparer Build()
        {
            return new EnhancedObjectComparer(_config);
        }
    }
}