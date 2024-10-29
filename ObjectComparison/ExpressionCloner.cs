using System.Collections;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;

namespace ObjectComparison;

/// <summary>
/// Advanced cloning functionality using expression trees
/// </summary>
internal class ExpressionCloner
{
    private readonly ComparisonConfig _config;
    private readonly HashSet<object> _clonedObjects = new();
    private readonly Dictionary<Type, Func<object, object>> _customCloners = new();

    public ExpressionCloner(ComparisonConfig config)
    {
        _config = config;
        InitializeCustomCloners();
    }

    private void InitializeCustomCloners()
    {
        // Add custom cloners for specific types
        _customCloners[typeof(DateTime)] = obj => ((DateTime)obj);
        _customCloners[typeof(string)] = obj => obj;
        _customCloners[typeof(decimal)] = obj => obj;
        _customCloners[typeof(Guid)] = obj => obj;
    }

    public T Clone<T>(T obj)
    {
        if (obj == null) return default;

        var type = obj.GetType();
        if (_customCloners.TryGetValue(type, out var customCloner))
        {
            return (T)customCloner(obj);
        }

        return (T)CloneObject(obj);
    }

    private object CloneObject(object obj)
    {
        if (obj == null) return null;

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
            // Handle collections
            if (metadata.IsCollection)
            {
                return CloneCollection(obj, type, metadata);
            }

            // Handle complex objects
            return CloneComplexObject(obj, type, metadata);
        }
        finally
        {
            _clonedObjects.Remove(obj);
        }
    }

    private object CloneCollection(object obj, Type type, TypeMetadata metadata)
    {
        var enumerable = (IEnumerable)obj;

        // Handle arrays
        if (type.IsArray)
        {
            var array = (Array)obj;
            var elementType = type.GetElementType();
            var clone = Array.CreateInstance(elementType, array.Length);

            for (int i = 0; i < array.Length; i++)
            {
                clone.SetValue(CloneObject(array.GetValue(i)), i);
            }

            return clone;
        }

        // Handle generic collections
        if (metadata.ItemType != null)
        {
            var listType = typeof(List<>).MakeGenericType(metadata.ItemType);
            var list = (IList)Activator.CreateInstance(listType);

            foreach (var item in enumerable)
            {
                list.Add(CloneObject(item));
            }

            // If the original was a List<T>, return as is
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return list;
            }

            // Try to convert to the original collection type
            try
            {
                var constructor = type.GetConstructor(new[]
                    { typeof(IEnumerable<>).MakeGenericType(metadata.ItemType) });
                if (constructor != null)
                {
                    return constructor.Invoke(new[] { list });
                }
            }
            catch (Exception ex)
            {
                _config.Logger?.LogWarning(ex, "Failed to convert cloned collection to type {Type}", type.Name);
            }

            return list;
        }

        // Handle non-generic collections
        var nonGenericList = new ArrayList();
        foreach (var item in enumerable)
        {
            nonGenericList.Add(CloneObject(item));
        }

        return nonGenericList;
    }

    private object CloneComplexObject(object obj, Type type, TypeMetadata metadata)
    {
        // Create instance
        var clone = CreateInstance(type);
        if (clone == null) return null;

        // Clone properties
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

        // Clone fields
        if (_config.ComparePrivateFields)
        {
            foreach (var field in metadata.Fields)
            {
                if (_config.ExcludedProperties.Contains(field.Name)) continue;

                try
                {
                    var value = field.GetValue(obj);
                    var clonedValue = CloneObject(value);
                    field.SetValue(clone, clonedValue);
                }
                catch (Exception ex)
                {
                    _config.Logger?.LogWarning(ex, "Failed to clone field {Field} of type {Type}",
                        field.Name, type.Name);
                }
            }
        }

        return clone;
    }

    private static object CreateInstance(Type type)
    {
        try
        {
            // Try to get the parameterless constructor
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor != null)
            {
                return Activator.CreateInstance(type);
            }

            // If no parameterless constructor exists, try to create uninitialized object
            return FormatterServices.GetUninitializedObject(type);
        }
        catch (Exception ex)
        {
            throw new ComparisonException($"Failed to create instance of type {type.Name}", "", ex);
        }
    }
}