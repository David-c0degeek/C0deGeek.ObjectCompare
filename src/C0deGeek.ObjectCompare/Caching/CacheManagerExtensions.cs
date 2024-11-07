using System.Reflection;

namespace C0deGeek.ObjectCompare.Caching;

/// <summary>
/// Extensions for configuring cache behavior
/// </summary>
public static class CacheManagerExtensions
{
    public static TValue GetOrAddWithFactory<TValue>(
        this CacheManager cache,
        string key,
        Func<CancellationToken, Task<TValue>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        async Task<TValue> AsyncFactory()
        {
            return await factory(cancellationToken);
        }

        return cache.GetOrAdd(key, 
            () => AsyncFactory().GetAwaiter().GetResult(), 
            expiration);
    }

    public static async Task<TValue> GetOrAddWithFactoryAsync<TValue>(
        this CacheManager cache,
        string key,
        Func<CancellationToken, Task<TValue>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        return await cache.GetOrAddAsync(key, 
            () => factory(cancellationToken), 
            expiration);
    }

    public static IEnumerable<KeyValuePair<string, object>> GetSnapshot(
        this CacheManager cache)
    {
        return cache.GetType()
                   .GetField("_caches", BindingFlags.NonPublic | BindingFlags.Instance)
                   ?.GetValue(cache) as IEnumerable<KeyValuePair<string, object>> 
               ?? [];
    }
}