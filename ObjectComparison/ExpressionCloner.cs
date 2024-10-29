using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace ObjectComparison;

public sealed class ExpressionCloner(ComparisonConfig config)
{
    private readonly ComparisonConfig _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly HashSet<object> _clonedObjects = [];
    private readonly Dictionary<Type, Func<object, object>> _customCloners = InitializeCustomCloners();
    private readonly ObjectCloneCache _cloneCache = new();

    private static Dictionary<Type, Func<object, object>> InitializeCustomCloners()
    {
        return new Dictionary<Type, Func<object, object>>
        {
            { typeof(DateTime), obj => obj },
            { typeof(string), obj => obj },
            { typeof(decimal), obj => obj },
            { typeof(Guid), obj => obj },
            // Add other immutable types as needed
        };
    }

    public T? Clone<T>(T? obj)
    {
        if (obj is null) return default;

        var type = obj.GetType();
        if (_customCloners.TryGetValue(type, out var customCloner))
        {
            return (T)customCloner(obj);
        }

        return (T)CloneObject(obj)!;
    }

    private object? CloneObject(object? obj)
    {
        if (obj is null) return null;

        var type = obj.GetType();
        var metadata = TypeCache.GetMetadata(type, _config.UseCachedMetadata);

        // Handle simple types
        if (metadata.IsSimpleType)
        {
            return obj;
        }

        // Check for circular references
        if (!_clonedObjects.Add(obj))
        {
            _config.Logger?.LogWarning("Circular reference detected while cloning type {Type}", type.Name);
            return obj;
        }

        try
        {
            return metadata.IsCollection
                ? CloneCollection(obj, type)
                : CloneComplexObject(obj, type, metadata);
        }
        finally
        {
            _clonedObjects.Remove(obj);
        }
    }

    private sealed class ObjectCloneCache
    {
        private readonly ConcurrentDictionary<Type, Func<object, object>> _cloneFuncs = new();

        public Func<object, object> GetOrCreateCloneFunc(Type type)
        {
            return _cloneFuncs.GetOrAdd(type, CreateCloneExpression);
        }

        private static Func<object, object> CreateCloneExpression(Type type)
        {
            var parameter = Expression.Parameter(typeof(object), "obj");
            var convertedParameter = Expression.Convert(parameter, type);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var bindings = properties
                .Where(p => p is { CanRead: true, CanWrite: true })
                .Select(p => Expression.Bind(
                    p,
                    Expression.Property(convertedParameter, p)
                ));

            var memberInit = Expression.MemberInit(
                Expression.New(type),
                bindings
            );

            var convertedResult = Expression.Convert(memberInit, typeof(object));
            return Expression.Lambda<Func<object, object>>(convertedResult, parameter).Compile();
        }
    }

    private object CloneCollection(object obj, Type type)
    {
        if (obj is not IEnumerable enumerable)
        {
            throw new ArgumentException($"Object of type {type.Name} is not enumerable");
        }

        // Handle arrays
        if (type.IsArray)
        {
            return CloneArray(enumerable, type);
        }

        // Handle generic collections
        var metadata = TypeCache.GetMetadata(type, _config.UseCachedMetadata);
        if (metadata.ItemType is not null)
        {
            return CloneGenericCollection(enumerable, type, metadata.ItemType);
        }

        // Handle non-generic collections
        return CloneNonGenericCollection(enumerable);
    }

    private object CloneArray(IEnumerable source, Type arrayType)
    {
        var elementType = arrayType.GetElementType() ?? 
                          throw new ArgumentException($"Could not get element type for array type {arrayType.Name}");
        
        var sourceArray = source.Cast<object>().ToArray();
        var array = Array.CreateInstance(elementType, sourceArray.Length);

        for (var i = 0; i < sourceArray.Length; i++)
        {
            var clonedElement = CloneObject(sourceArray[i]);
            array.SetValue(clonedElement, i);
        }

        return array;
    }

    private object CloneGenericCollection(IEnumerable source, Type collectionType, Type itemType)
    {
        var listType = typeof(List<>).MakeGenericType(itemType);
        var list = (IList)Activator.CreateInstance(listType)!;

        foreach (var item in source)
        {
            list.Add(CloneObject(item));
        }

        // If the original was a List<T>, return as is
        if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(List<>))
        {
            return list;
        }

        // Try to convert to the original collection type
        try
        {
            var constructor = collectionType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(itemType)]);
            if (constructor is not null)
            {
                return constructor.Invoke([list]);
            }
        }
        catch (Exception ex)
        {
            _config.Logger?.LogWarning(ex, "Failed to convert cloned collection to type {Type}", collectionType.Name);
        }

        return list;
    }

    private object CloneNonGenericCollection(IEnumerable source)
    {
        var list = new ArrayList();
        foreach (var item in source)
        {
            list.Add(CloneObject(item));
        }
        return list;
    }

    private object CloneComplexObject(object obj, Type type, TypeMetadata metadata)
    {
        var clone = CreateInstance(type);
        if (clone is null)
        {
            throw new ComparisonException($"Failed to create instance of type {type.Name}");
        }

        CloneObjectProperties(obj, clone);
        return clone;
    }

    private static object? CreateInstance(Type type)
    {
        try
        {
            // Try to get the parameterless constructor
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor is not null)
            {
                return Activator.CreateInstance(type);
            }

            // If no parameterless constructor exists, use alternative instantiation
            return RuntimeHelpers.GetUninitializedObject(type);
        }
        catch (Exception ex)
        {
            throw new ComparisonException($"Failed to create instance of type {type.Name}", "", ex);
        }
    }
}