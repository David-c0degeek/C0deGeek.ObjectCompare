using System.Collections.Concurrent;
using System.Reflection;
using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Comparison.Exceptions;
using C0deGeek.ObjectCompare.Metadata;
using Microsoft.Extensions.Logging;

namespace C0deGeek.ObjectCompare.Cloning;

/// <summary>
/// Strategy for cloning complex objects and classes
/// </summary>
public class ComplexTypeCloner(ComparisonConfig config, ILogger? logger = null) : CloneStrategyBase(logger)
{
    private readonly Dictionary<Type, ICloneStrategy> _subStrategies = InitializeSubStrategies(config);
    private readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();
    private readonly ExpressionCloner _expressionCloner = new(config);

    private static Dictionary<Type, ICloneStrategy> InitializeSubStrategies(ComparisonConfig config)
    {
        return new Dictionary<Type, ICloneStrategy>
        {
            { typeof(ValueType), new SimpleTypeCloner() },
            { typeof(IEnumerable), new CollectionCloner(config) }
        };
    }

    public override bool CanHandle(Type type)
    {
        return type is { IsPrimitive: false, IsEnum: false } && 
               type != typeof(string) &&
               !typeof(IEnumerable).IsAssignableFrom(type);
    }

    public override int Priority => 0;

    public override object? Clone(object? obj, CloneContext context)
    {
        if (obj == null) return null;

        var type = obj.GetType();
        LogCloning(nameof(ComplexTypeCloner), type);

        // Check for circular references
        if (context.TryGetExistingClone(obj, out var existingClone))
        {
            return existingClone;
        }

        try
        {
            // Create new instance and register it before cloning properties
            // to handle circular references
            var clone = CreateInstance(type);
            context.RegisterClone(obj, clone);

            CloneProperties(obj, clone, type, context);
            CloneFields(obj, clone, type, context);

            return clone;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cloning complex object of type {Type}", type.Name);
            throw new ComparisonException(
                ExceptionHelper.CreateCloneFailureMessage(type), "", ex);
        }
    }

    private void CloneProperties(object source, object target, Type type, CloneContext context)
    {
        var properties = GetCachedProperties(type);

        foreach (var prop in properties)
        {
            if (!ShouldCloneProperty(prop)) continue;

            try
            {
                var value = prop.GetValue(source);
                var clonedValue = CloneValue(value, prop.PropertyType, context);
                
                prop.SetValue(target, clonedValue);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, 
                    "Failed to clone property {Property} of type {Type}",
                    prop.Name, type.Name);
            }
        }
    }

    private void CloneFields(object source, object target, Type type, CloneContext context)
    {
        if (!context.Config.ComparePrivateFields) return;

        var metadata = TypeCache.GetMetadata(type, context.Config.UseCachedMetadata);
        foreach (var field in metadata.Fields)
        {
            if (context.Config.ExcludedProperties.Contains(field.Name)) continue;

            try
            {
                var value = field.GetValue(source);
                var clonedValue = CloneValue(value, field.FieldType, context);
                
                field.SetValue(target, clonedValue);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, 
                    "Failed to clone field {Field} of type {Type}",
                    field.Name, type.Name);
            }
        }
    }

    private object? CloneValue(object? value, Type type, CloneContext context)
    {
        if (value == null) return null;

        foreach (var strategy in _subStrategies)
        {
            if (strategy.Key.IsAssignableFrom(type))
            {
                return strategy.Value.Clone(value, context);
            }
        }

        return Clone(value, context);
    }

    private PropertyInfo[] GetCachedProperties(Type type)
    {
        return _propertyCache.GetOrAdd(type, t => 
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
    }

    private bool ShouldCloneProperty(PropertyInfo prop)
    {
        if (!prop.CanRead || !prop.CanWrite) return false;
        if (prop.GetCustomAttribute<NonClonedAttribute>() != null) return false;
        
        return true;
    }
}