using System.Collections.Concurrent;
using C0deGeek.ObjectCompare.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace C0deGeek.ObjectCompare.Caching;

/// <summary>
/// Provides centralized cache management with memory pressure monitoring
/// </summary>
public sealed class CacheManager : IDisposable
{
    private readonly ConcurrentDictionary<string, object> _caches = new();
    private readonly ILogger _logger;
    private readonly MemoryCache _memoryCache;
    private readonly MemoryCacheOptions _options;
    private bool _disposed;

    public CacheManager(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _options = new MemoryCacheOptions
        {
            SizeLimit = GetDefaultCacheSize(),
            ExpirationScanFrequency = TimeSpan.FromMinutes(5)
        };
        _memoryCache = new MemoryCache(_options);

        // Start monitoring memory pressure
        StartMemoryMonitoring();
    }

    public TValue GetOrAdd<TValue>(string cacheKey, 
        Func<TValue> valueFactory, 
        TimeSpan? expiration = null)
    {
        ThrowIfDisposed();
        Guard.ThrowIfNullOrEmpty(cacheKey, nameof(cacheKey));
        Guard.ThrowIfNull(valueFactory, nameof(valueFactory));

        try
        {
            return (TValue)_caches.GetOrAdd(cacheKey, _ =>
            {
                var value = valueFactory();
                if (value != null)
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSize(1) // Cost of one cache entry
                        .RegisterPostEvictionCallback(OnCacheEntryEvicted);

                    if (expiration.HasValue)
                    {
                        cacheEntryOptions.SetAbsoluteExpiration(expiration.Value);
                    }

                    _memoryCache.Set(cacheKey, value, cacheEntryOptions);
                    _logger.LogDebug("Added item to cache with key {Key}", cacheKey);
                }
                return value!;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing cache for key {Key}", cacheKey);
            throw;
        }
    }

    public async Task<TValue> GetOrAddAsync<TValue>(string cacheKey, 
        Func<Task<TValue>> valueFactory, 
        TimeSpan? expiration = null)
    {
        ThrowIfDisposed();
        Guard.ThrowIfNullOrEmpty(cacheKey, nameof(cacheKey));
        Guard.ThrowIfNull(valueFactory, nameof(valueFactory));

        if (_caches.TryGetValue(cacheKey, out var existingValue))
        {
            return (TValue)existingValue;
        }

        try
        {
            var value = await valueFactory();
            return GetOrAdd(cacheKey, () => value, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing cache asynchronously for key {Key}", 
                cacheKey);
            throw;
        }
    }

    public bool TryGetValue<TValue>(string key, out TValue? value)
    {
        ThrowIfDisposed();
        value = default;

        if (!_caches.TryGetValue(key, out var cachedValue))
            return false;

        value = (TValue)cachedValue;
        return true;
    }

    public void Remove(string key)
    {
        if (_disposed) return;

        if (_caches.TryRemove(key, out _))
        {
            _memoryCache.Remove(key);
            _logger.LogDebug("Removed item from cache with key {Key}", key);
        }
    }

    public void Clear()
    {
        if (_disposed) return;

        _caches.Clear();
        ((IDisposable)_memoryCache).Dispose();
        
        _logger.LogInformation("Cache cleared");
    }

    private void StartMemoryMonitoring()
    {
        Task.Run(async () =>
        {
            while (!_disposed)
            {
                try
                {
                    if (IsUnderMemoryPressure())
                    {
                        TrimCache();
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring memory pressure");
                }
            }
        });
    }

    private static bool IsUnderMemoryPressure()
    {
        var totalMemory = GC.GetTotalMemory(false);
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        var memoryLoad = (double)totalMemory / gcMemoryInfo.TotalAvailableMemoryBytes;

        return memoryLoad > 0.8; // 80% threshold
    }

// In CacheManager.cs
    private void TrimCache()
    {
        try
        {
            var itemsToRemove = (int)(_caches.Count * 0.2); // Remove 20% of items
            var oldestItems = _caches.Take(itemsToRemove); // Simply take oldest by insertion order

            foreach (var item in oldestItems)
            {
                Remove(item.Key);
            }

            _logger.LogInformation("Trimmed {Count} items from cache due to memory pressure", 
                itemsToRemove);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error trimming cache");
        }
    }

    private void OnCacheEntryEvicted(object key, object value, 
        EvictionReason reason, object state)
    {
        _caches.TryRemove(key.ToString()!, out _);
        _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", key, reason);
    }

    private static long GetDefaultCacheSize()
    {
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        return (long)(gcMemoryInfo.TotalAvailableMemoryBytes * 0.1); // 10% of available memory
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CacheManager));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Clear();
        _disposed = true;
    }

    public class CacheEntry<T>
    {
        public T Value { get; }
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset? ExpiresAt { get; }

        public CacheEntry(T value, TimeSpan? expiration = null)
        {
            Value = value;
            CreatedAt = DateTimeOffset.UtcNow;
            ExpiresAt = expiration.HasValue 
                ? CreatedAt.Add(expiration.Value) 
                : null;
        }

        public bool IsExpired => 
            ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;
    }
}