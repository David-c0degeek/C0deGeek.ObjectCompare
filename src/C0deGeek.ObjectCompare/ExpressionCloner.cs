using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace C0deGeek.ObjectCompare;

public sealed class ExpressionCloner(ComparisonConfig config)
{
    private readonly ComparisonConfig _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly Dictionary<Type, Func<object, object>> _customCloners = InitializeCustomCloners();
    private readonly Dictionary<object, object> _circularReferenceTracker = new(new ReferenceEqualityComparer());
    private static readonly ObjectCloneCache CloneFuncCache = new();

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }

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

        // Handle simple types directly
        if (metadata.IsSimpleType)
        {
            return obj;
        }

        // Check circular reference
        if (_circularReferenceTracker.TryGetValue(obj, out var existingClone))
        {
            return existingClone;
        }

        // Create new instance
        var clone = CreateInstance(type);

        // Add to tracker BEFORE recursing into properties
        _circularReferenceTracker[obj] = clone ?? throw new ComparisonException($"Failed to create instance of type {type.Name}");

        try
        {
            // Clone the object's contents
            if (metadata.IsCollection)
            {
                return CloneCollection(obj, type);
            }

            CloneObjectProperties(obj, clone);
            return clone;
        }
        catch
        {
            _circularReferenceTracker.Remove(obj);
            throw;
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
                
                // Clone the value (will return from tracker if circular)
                var clonedValue = CloneObject(value);
                
                // Set the value directly - no need for CreateSafeValue here
                setter(target, clonedValue);
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
                
                // Clone the value (will return from tracker if circular)
                var clonedValue = CloneObject(value);
                
                // Set the value directly
                field.SetValue(target, clonedValue);
            }
            catch (Exception ex)
            {
                _config.Logger?.LogWarning(ex, "Failed to clone field {Field} of type {Type}",
                    field.Name, source.GetType().Name);
            }
        }
    }

    private static object CreateSafeValue(object? value, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        if (value != null) return value;

        // Handle nullable value types
        var nullableUnderlyingType = Nullable.GetUnderlyingType(targetType);
        if (nullableUnderlyingType != null)
        {
            // For nullable types, we can safely return null by casting
            return null!;
        }

        // For reference types, return null with cast to remove nullability warning
        if (!targetType.IsValueType) return null!;

        // For non-nullable value types, create a default instance
        try
        {
            return Activator.CreateInstance(targetType) ??
                   throw new InvalidOperationException($"Failed to create instance of type {targetType.Name}");
        }
        catch
        {
            var uninitialized = RuntimeHelpers.GetUninitializedObject(targetType);
            if (uninitialized == null)
            {
                throw new InvalidOperationException(
                    $"Failed to create uninitialized instance of type {targetType.Name}");
            }

            return uninitialized;
        }
    }
}