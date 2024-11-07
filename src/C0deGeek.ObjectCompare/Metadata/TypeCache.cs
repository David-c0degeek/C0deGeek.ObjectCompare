using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace C0deGeek.ObjectCompare.Metadata;

/// <summary>
/// Cache for type metadata and compiled expressions
/// </summary>
internal static class TypeCache
{
    private const int DefaultCacheSize = 1000;
    private static readonly ConcurrentDictionary<Type, TypeMetadata> MetadataCache = new();
    private static readonly ConcurrentDictionary<(Type Type, string Name), Func<object, object>> PropertyGetters = new();
    private static readonly ConcurrentDictionary<(Type Type, string Name), Action<object, object>> PropertySetters = new();
    private static readonly ReaderWriterLockSlim CacheLock = new();

    public static TypeMetadata GetMetadata(Type type, bool useCache)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!useCache)
        {
            return new TypeMetadata(type);
        }

        return MetadataCache.GetOrAdd(type, t =>
        {
            TrimCacheIfNeeded();
            return new TypeMetadata(t);
        });
    }

    public static Func<object, object> GetPropertyGetter(Type type, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(propertyName);

        return PropertyGetters.GetOrAdd((type, propertyName), key => CreatePropertyGetter(key.Type, key.Name));
    }

    public static Action<object, object> GetPropertySetter(Type type, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(propertyName);

        return PropertySetters.GetOrAdd((type, propertyName), key => CreatePropertySetter(key.Type, key.Name));
    }

    private static Func<object, object> CreatePropertyGetter(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName) ?? 
            throw new ArgumentException($"Property {propertyName} not found on type {type.Name}");

        var parameter = Expression.Parameter(typeof(object), "obj");
        var convertedParameter = Expression.Convert(parameter, type);
        var propertyAccess = Expression.Property(convertedParameter, property);
        var convertedProperty = Expression.Convert(propertyAccess, typeof(object));

        try
        {
            return Expression.Lambda<Func<object, object>>(convertedProperty, parameter).Compile();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create property getter for {propertyName} on type {type.Name}", ex);
        }
    }

    private static Action<object, object> CreatePropertySetter(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName) ?? 
            throw new ArgumentException($"Property {propertyName} not found on type {type.Name}");

        if (!property.CanWrite)
        {
            throw new ArgumentException($"Property {propertyName} on type {type.Name} is read-only");
        }

        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var valueParam = Expression.Parameter(typeof(object), "value");
        var convertedInstance = Expression.Convert(instanceParam, type);
        var convertedValue = Expression.Convert(valueParam, property.PropertyType);
        var propertyAccess = Expression.Property(convertedInstance, property);
        var assign = Expression.Assign(propertyAccess, convertedValue);

        try
        {
            return Expression.Lambda<Action<object, object>>(assign, instanceParam, valueParam).Compile();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create property setter for {propertyName} on type {type.Name}", ex);
        }
    }

    private static void TrimCacheIfNeeded()
    {
        if (MetadataCache.Count < DefaultCacheSize) return;

        using var writeLock = new WriteLockScope(CacheLock);
        var itemsToRemove = MetadataCache.Count - (DefaultCacheSize * 3 / 4);
        if (itemsToRemove <= 0) return;

        var oldest = MetadataCache.Take(itemsToRemove).ToList();
        foreach (var item in oldest)
        {
            MetadataCache.TryRemove(item.Key, out _);
        }
    }

    private sealed class WriteLockScope : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;
        private bool _disposed;

        public WriteLockScope(ReaderWriterLockSlim @lock)
        {
            _lock = @lock;
            _lock.EnterWriteLock();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _lock.ExitWriteLock();
            _disposed = true;
        }
    }

    public static void ClearCache()
    {
        using var writeLock = new WriteLockScope(CacheLock);
        MetadataCache.Clear();
        PropertyGetters.Clear();
        PropertySetters.Clear();
    }
}