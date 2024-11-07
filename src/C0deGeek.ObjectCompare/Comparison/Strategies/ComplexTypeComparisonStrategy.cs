using System.Dynamic;
using System.Reflection;
using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Comparison.Exceptions;
using C0deGeek.ObjectCompare.Dynamic;
using C0deGeek.ObjectCompare.Metadata;
using C0deGeek.ObjectCompare.Models;
using Microsoft.Extensions.Logging;

namespace C0deGeek.ObjectCompare.Comparison.Strategies;

/// <summary>
/// Strategy for comparing complex objects and classes
/// </summary>
public class ComplexTypeComparisonStrategy(ComparisonConfig config) : ComparisonStrategyBase(config)
{
    private readonly DynamicObjectComparer _dynamicComparer = new(config);

    public override bool CanHandle(Type type)
    {
        return !type.IsPrimitive && 
               !type.IsEnum && 
               type != typeof(string) &&
               !typeof(IEnumerable).IsAssignableFrom(type);
    }

    public override int Priority => 0;

    public override bool Compare(object? obj1, object? obj2, string path, 
        ComparisonResult result, ComparisonContext context)
    {
        LogComparison(nameof(ComplexTypeComparisonStrategy), path, obj1, obj2);

        if (HandleNulls(obj1, obj2, path, result)) return result.AreEqual;

        var type = obj1!.GetType();

        // Check for circular references
        var pair = new ComparisonPair(obj1, obj2!);
        if (!context.ComparedObjects.Add(pair))
        {
            return true; // Already compared these objects
        }

        try
        {
            // Handle dynamic objects
            if (obj1 is IDynamicMetaObjectProvider || obj2 is IDynamicMetaObjectProvider)
            {
                return _dynamicComparer.AreEqual(obj1, obj2, path, result);
            }

            // Handle custom comparers
            if (Config.CustomComparers.TryGetValue(type, out var customComparer))
            {
                if (!customComparer.AreEqual(obj1, obj2, Config))
                {
                    result.AddDifference(
                        $"Custom comparison failed for type {type.Name}", path);
                    result.AreEqual = false;
                    return false;
                }
                return true;
            }

            return CompareProperties(obj1, obj2, type, path, result, context);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error comparing objects of type {Type} at path {Path}", 
                type.Name, path);
            throw new ComparisonException(
                $"Error comparing objects of type {type.Name}", path, ex);
        }
    }

    private bool CompareProperties(object obj1, object obj2, Type type, string path, 
        ComparisonResult result, ComparisonContext context)
    {
        var metadata = TypeCache.GetMetadata(type, Config.UseCachedMetadata);
        var isEqual = true;

        foreach (var prop in metadata.Properties)
        {
            if (!ShouldCompareProperty(prop)) continue;

            try
            {
                var propertyPath = $"{path}.{prop.Name}";
                var value1 = prop.GetValue(obj1);
                var value2 = prop.GetValue(obj2);

                if (context.CurrentDepth >= Config.MaxDepth)
                {
                    throw new MaximumDepthExceededException(propertyPath, Config.MaxDepth, type);
                }

                if (Config.DeepComparison)
                {
                    var strategy = GetPropertyStrategy(prop.PropertyType);
                    if (!strategy.Compare(value1, value2, propertyPath, result, context))
                    {
                        isEqual = false;
                    }
                }
                else if (!AreValuesEqual(value1, value2))
                {
                    result.AddDifference(
                        $"Property {prop.Name} differs: {value1} != {value2}", 
                        propertyPath);
                    isEqual = false;
                }
            }
            catch (Exception ex) when (ex is not ComparisonException)
            {
                Logger.LogError(ex, "Error comparing property {Property} of type {Type}", 
                    prop.Name, type.Name);
                result.AddDifference(
                    $"Error comparing property {prop.Name}: {ex.Message}", 
                    $"{path}.{prop.Name}");
                isEqual = false;
            }
        }

        if (Config.ComparePrivateFields)
        {
            isEqual = CompareFields(obj1, obj2, type, path, result, context) && isEqual;
        }

        result.AreEqual = isEqual;
        return isEqual;
    }

    private bool CompareFields(object obj1, object obj2, Type type, string path, 
        ComparisonResult result, ComparisonContext context)
    {
        var metadata = TypeCache.GetMetadata(type, Config.UseCachedMetadata);
        var isEqual = true;

        foreach (var field in metadata.Fields)
        {
            if (!Config.ComparePrivateFields && field.IsPrivate) continue;
            if (Config.ExcludedProperties.Contains(field.Name)) continue;

            try
            {
                var fieldPath = $"{path}.{field.Name}";
                var value1 = field.GetValue(obj1);
                var value2 = field.GetValue(obj2);

                if (context.CurrentDepth >= Config.MaxDepth)
                {
                    throw new MaximumDepthExceededException(fieldPath, Config.MaxDepth, type);
                }

                if (Config.DeepComparison)
                {
                    var strategy = GetPropertyStrategy(field.FieldType);
                    if (!strategy.Compare(value1, value2, fieldPath, result, context))
                    {
                        isEqual = false;
                    }
                }
                else if (!AreValuesEqual(value1, value2))
                {
                    result.AddDifference(
                        $"Field {field.Name} differs: {value1} != {value2}", 
                        fieldPath);
                    isEqual = false;
                }
            }
            catch (Exception ex) when (ex is not ComparisonException)
            {
                Logger.LogError(ex, "Error comparing field {Field} of type {Type}", 
                    field.Name, type.Name);
                result.AddDifference(
                    $"Error comparing field {field.Name}: {ex.Message}", 
                    $"{path}.{field.Name}");
                isEqual = false;
            }
        }

        return isEqual;
    }

    private bool ShouldCompareProperty(PropertyInfo prop)
    {
        if (Config.ExcludedProperties.Contains(prop.Name)) return false;
        if (!prop.CanRead) return false;
        if (!Config.CompareReadOnlyProperties && !prop.CanWrite) return false;
        
        return true;
    }

    private static bool AreValuesEqual(object? value1, object? value2)
    {
        if (ReferenceEquals(value1, value2)) return true;
        if (value1 == null || value2 == null) return false;

        try
        {
            return value1.Equals(value2);
        }
        catch (Exception)
        {
            // If Equals throws an exception, try reverse comparison
            try
            {
                return value2.Equals(value1);
            }
            catch (Exception)
            {
                // If both comparisons fail, consider values not equal
                return false;
            }
        }
    }

    private IComparisonStrategy GetPropertyStrategy(Type propertyType)
    {
        if (propertyType.IsPrimitive || propertyType == typeof(string) || propertyType.IsEnum)
        {
            return new SimpleTypeComparisonStrategy(Config);
        }

        if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
        {
            return new CollectionComparisonStrategy(Config);
        }

        return this;
    }
}