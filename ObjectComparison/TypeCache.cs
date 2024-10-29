using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace ObjectComparison;

/// <summary>
/// Cache for type metadata and compiled expressions
/// </summary>
internal static class TypeCache
{
    private static readonly ConcurrentDictionary<Type, TypeMetadata> MetadataCache = new();
    private static readonly ConcurrentDictionary<Type, Func<object, object>> CloneFuncs = new();
    private static readonly ConcurrentDictionary<(Type, string), Func<object, object>> PropertyGetters = new();
    private static readonly ConcurrentDictionary<(Type, string), Action<object, object>> PropertySetters = new();

    public static TypeMetadata GetMetadata(Type type, bool useCache)
    {
        return !useCache 
            ? new TypeMetadata(type) 
            : MetadataCache.GetOrAdd(type, t => new TypeMetadata(t));
    }

    public static Func<object, object> GetCloneFunc(Type type)
    {
        return CloneFuncs.GetOrAdd(type, CreateCloneExpression);
    }

    public static Func<object, object> GetPropertyGetter(Type type, string propertyName)
    {
        return PropertyGetters.GetOrAdd((type, propertyName), key => CreatePropertyGetter(key.Item1, key.Item2));
    }

    public static Action<object, object> GetPropertySetter(Type type, string propertyName)
    {
        return PropertySetters.GetOrAdd((type, propertyName), key => CreatePropertySetter(key.Item1, key.Item2));
    }

    private static Func<object, object> CreateCloneExpression(Type type)
    {
        // Implementation will be shown in the cloning section
        throw new NotImplementedException();
    }

    private static Func<object, object> CreatePropertyGetter(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        if (property == null)
        {
            throw new ArgumentException($"Property {propertyName} not found on type {type.Name}");
        }

        var parameter = Expression.Parameter(typeof(object), "obj");
        var convertedParameter = Expression.Convert(parameter, type);
        var propertyAccess = Expression.Property(convertedParameter, property);
        var convertedProperty = Expression.Convert(propertyAccess, typeof(object));

        return Expression.Lambda<Func<object, object>>(convertedProperty, parameter).Compile();
    }

    private static Action<object, object> CreatePropertySetter(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        if (property == null)
        {
            throw new ArgumentException($"Property {propertyName} not found on type {type.Name}");
        }

        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var valueParam = Expression.Parameter(typeof(object), "value");
        var convertedInstance = Expression.Convert(instanceParam, type);
        var convertedValue = Expression.Convert(valueParam, property.PropertyType);
        var propertyAccess = Expression.Property(convertedInstance, property);
        var assign = Expression.Assign(propertyAccess, convertedValue);

        return Expression.Lambda<Action<object, object>>(assign, instanceParam, valueParam).Compile();
    }
}