using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace C0deGeek.ObjectCompare.Resources;

/// <summary>
/// Manages a pool of resources for comparison operations
/// </summary>
public sealed class ComparisonResourcePool(int maxConcurrency = -1, ILogger? logger = null) : IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _comparers = new();
    private readonly ConcurrentBag<IDisposable> _resources = [];
    private readonly SemaphoreSlim _semaphore = new(
        maxConcurrency > 0 ? maxConcurrency : Environment.ProcessorCount);
    private readonly ILogger _logger = logger ?? NullLogger.Instance;
    private bool _disposed;

    public async Task<T> AcquireComparerAsync<T>() where T : class, IDisposable
    {
        ThrowIfDisposed();

        await _semaphore.WaitAsync();
        try
        {
            var comparer = (T)_comparers.GetOrAdd(typeof(T), _ =>
            {
                var instance = Activator.CreateInstance<T>();
                _resources.Add(instance);
                _logger.LogDebug("Created new instance of {Type}", typeof(T).Name);
                return instance;
            });
            
            return comparer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire comparer of type {Type}", typeof(T).Name);
            _semaphore.Release();
            throw;
        }
    }

    public void ReleaseComparer<T>(T comparer) where T : class
    {
        if (_disposed) return;
        
        try
        {
            _semaphore.Release();
            _logger.LogDebug("Released comparer of type {Type}", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error releasing comparer of type {Type}", typeof(T).Name);
        }
    }

    public async Task<ResourceScope<T>> AcquireResourceScopeAsync<T>() where T : class, IDisposable
    {
        var resource = await AcquireComparerAsync<T>();
        return new ResourceScope<T>(this, resource);
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var resource in _resources)
        {
            try
            {
                resource.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing resource");
            }
        }

        _semaphore.Dispose();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ComparisonResourcePool));
        }
    }
}