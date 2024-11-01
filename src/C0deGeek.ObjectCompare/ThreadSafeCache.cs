using System.Collections.Concurrent;

namespace C0deGeek.ObjectCompare;

/// <summary>
/// Thread-safe cache management
/// </summary>
internal static class ThreadSafeCache
{
    private static readonly ConcurrentDictionary<Type, TypeMetadata> MetadataCache = new();
    private static readonly ConcurrentDictionary<(Type, string), Func<object, object>> PropertyGetters = new();

    private static readonly ConcurrentDictionary<Type, Func<object, IDictionary<string, object>>> DynamicAccessors =
        new();

    private static readonly ReaderWriterLockSlim CacheLock = new();
    private const int MaxCacheSize = 1000;

    public static void ClearCaches()
    {
        using (new WriteLockScope(CacheLock))
        {
            MetadataCache.Clear();
            PropertyGetters.Clear();
            DynamicAccessors.Clear();
        }
    }

    public static TypeMetadata GetOrAddMetadata(Type type, Func<Type, TypeMetadata> factory)
    {
        if (MetadataCache.Count >= MaxCacheSize)
        {
            // Implement cache cleanup if needed
            TrimCache();
        }

        return MetadataCache.GetOrAdd(type, factory);
    }

    private static void TrimCache()
    {
        using (new WriteLockScope(CacheLock))
        {
            // Remove least recently used items
            var itemsToRemove = MetadataCache.Count - (MaxCacheSize * 3 / 4);
            if (itemsToRemove <= 0) return;
            
            var oldest = MetadataCache.Take(itemsToRemove).ToList();
            foreach (var item in oldest)
            {
                MetadataCache.TryRemove(item.Key, out _);
            }
        }
    }

    private class WriteLockScope : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;

        public WriteLockScope(ReaderWriterLockSlim @lock)
        {
            _lock = @lock;
            _lock.EnterWriteLock();
        }

        public void Dispose()
        {
            _lock.ExitWriteLock();
        }
    }
}