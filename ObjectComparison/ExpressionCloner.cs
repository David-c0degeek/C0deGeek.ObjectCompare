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
            { typeof(Guid), obj => obj }
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
                : CloneComplexObject(obj, type);
        }
        finally
        {
            _clonedObjects.Remove(obj);
        }
    }

    private sealed class ObjectCloneCache
    {
        private readonly ConcurrentDictionary<Type, Func<object, object, ComparisonConfig, object>> _cloneFuncs = new();

        public Func<object, object, ComparisonConfig, object> GetOrCreateCloneFunc(Type type)
        {
            return _cloneFuncs.GetOrAdd(type, CreateCloneExpression);
        }

        private static Func<object, object, ComparisonConfig, object> CreateCloneExpression(Type type)
        {
            var sourceParam = Expression.Parameter(typeof(object), "source");
            var targetParam = Expression.Parameter(typeof(object), "target");
            var configParam = Expression.Parameter(typeof(ComparisonConfig), "config");

            var typedSource = Expression.Convert(sourceParam, type);
            var typedTarget = Expression.Convert(targetParam, type);

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p is { CanRead: true, CanWrite: true });

            var assignments = new List<Expression>();

            // Get CreateSafeValue method
            var createSafeValueMethod = typeof(ExpressionCloner).GetMethod(
                "CreateSafeValue",
                BindingFlags.NonPublic | BindingFlags.Static) ?? 
                throw new InvalidOperationException("CreateSafeValue method not found");

            foreach (var prop in properties)
            {
                var propAccess = Expression.Property(typedSource, prop);
                var targetPropAccess = Expression.Property(typedTarget, prop);

                // Create a safe value expression with null check
                var safeValueExpression = Expression.Call(
                    createSafeValueMethod,
                    propAccess,
                    Expression.Constant(prop.PropertyType, typeof(Type)));

                // Convert the result to the property type with null check
                var convertedValue = Expression.Convert(
                    Expression.Condition(
                        Expression.Equal(safeValueExpression, Expression.Constant(null)),
                        Expression.Default(prop.PropertyType),
                        Expression.Convert(safeValueExpression, prop.PropertyType)
                    ),
                    prop.PropertyType
                );

                assignments.Add(Expression.Assign(targetPropAccess, convertedValue));
            }

            assignments.Add(targetParam);
            var body = Expression.Block(assignments);

            return Expression.Lambda<Func<object, object, ComparisonConfig, object>>(
                body, sourceParam, targetParam, configParam).Compile();
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

    private Array CloneArray(IEnumerable source, Type arrayType)
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

    private ArrayList CloneNonGenericCollection(IEnumerable source)
    {
        var list = new ArrayList();
        foreach (var item in source)
        {
            list.Add(CloneObject(item));
        }

        return list;
    }

    private object CloneComplexObject(object obj, Type type)
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

    private void CloneObjectProperties(object source, object target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        var type = source.GetType();

        try
        {
            // Use cached clone function if available
            var cloneFunc = _cloneCache.GetOrCreateCloneFunc(type);
            cloneFunc(source, target, _config);
        }
        catch (Exception ex)
        {
            _config.Logger?.LogWarning(ex,
                "Failed to use cached clone function for type {Type}, falling back to reflection",
                type.Name);

            // Fall back to reflection-based cloning
            CloneObjectPropertiesReflection(source, target);
        }
    }

    private void CloneObjectPropertiesReflection(object source, object target)
    {
        var type = source.GetType();
        var metadata = TypeCache.GetMetadata(type, _config.UseCachedMetadata);

        foreach (var prop in metadata.Properties)
        {
            if (!prop.CanWrite) continue;
            if (_config.ExcludedProperties.Contains(prop.Name)) continue;

            try
            {
                var getter = TypeCache.GetPropertyGetter(type, prop.Name);
                var setter = TypeCache.GetPropertySetter(type, prop.Name);
                var value = getter(source);
                var clonedValue = CloneObject(value);

                // Create safe value for the property
                var safeValue = CreateSafeValue(clonedValue, prop.PropertyType);
                setter(target, safeValue);
            }
            catch (Exception ex)
            {
                _config.Logger?.LogWarning(ex, "Failed to clone property {Property} of type {Type}",
                    prop.Name, type.Name);
            }
        }

        if (_config.ComparePrivateFields)
        {
            CloneFields(source, target, metadata);
        }
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

                // Create safe value for the field
                var safeValue = CreateSafeValue(clonedValue, field.FieldType);
                field.SetValue(target, safeValue);
            }
            catch (Exception ex)
            {
                _config.Logger?.LogWarning(ex, "Failed to clone field {Field} of type {Type}",
                    field.Name, source.GetType().Name);
            }
        }
    }

    private static object? CreateSafeValue(object? value, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        if (value != null) return value;

        // Handle nullable value types
        var nullableUnderlyingType = Nullable.GetUnderlyingType(targetType);
        if (nullableUnderlyingType != null)
        {
            return null;
        }

        // For reference types, return null
        if (!targetType.IsValueType) return null;
        
        // For non-nullable value types, create a default instance
        try
        {
            return Activator.CreateInstance(targetType);
        }
        catch
        {
            return RuntimeHelpers.GetUninitializedObject(targetType);
        }
    }
}