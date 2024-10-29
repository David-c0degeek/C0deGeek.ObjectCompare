using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;

namespace ObjectComparison;

/// <summary>
/// Advanced cloning functionality using expression trees
/// </summary>
internal sealed class ExpressionCloner(ComparisonConfig config)
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

    private object? CloneObject(object obj)
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
                ? CloneCollection(obj, type, metadata)
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
            // Implementation details for creating clone expression
            // This would use expression trees to create efficient clone functions
            throw new NotImplementedException("Clone expression creation to be implemented");
        }
    }

    private object CloneCollection(object obj, Type type, TypeMetadata metadata)
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
            array.SetValue(CloneObject(sourceArray[i]), i);
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

        foreach (var prop in metadata.Properties)
        {
            if (!prop.CanWrite) continue;
            if (_config.ExcludedProperties.Contains(prop.Name)) continue;

            try
            {
                var getter = TypeCache.GetPropertyGetter(type, prop.Name);
                var setter = TypeCache.GetPropertySetter(type, prop.Name);
                var value = getter(obj);
                var clonedValue = CloneObject(value);
                setter(clone, clonedValue);
            }
            catch (Exception ex)
            {
                _config.Logger?.LogWarning(ex, "Failed to clone property {Property} of type {Type}",
                    prop.Name, type.Name);
            }
        }

        if (_config.ComparePrivateFields)
        {
            CloneFields(obj, clone, metadata);
        }

        return clone;
    }

    private void CloneFields(object source, object target, TypeMetadata metadata)
    {
        foreach (var field in metadata.Fields)
        {
            if (_config.ExcludedProperties.Contains(field.Name)) continue;

            try
            {
                var value = field.GetValue(source);
                var clonedValue = CloneObject(value);
                field.SetValue(target, clonedValue);
            }
            catch (Exception ex)
            {
                _config.Logger?.LogWarning(ex, "Failed to clone field {Field} of type {Type}",
                    field.Name, source.GetType().Name);
            }
        }
    }

    private static object? CreateInstance(Type type)
    {
        try
        {
            // Try to get the parameterless constructor
            var constructor = type.GetConstructor(Type.EmptyTypes);
            return constructor is not null
                ? Activator.CreateInstance(type)
                // If no parameterless constructor exists, use alternative instantiation
                : RuntimeHelpers.GetUninitializedObject(type);
        }
        catch (Exception ex)
        {
            throw new ComparisonException($"Failed to create instance of type {type.Name}", "", ex);
        }
    }
}