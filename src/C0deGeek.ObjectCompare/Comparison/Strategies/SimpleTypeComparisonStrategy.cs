using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Comparison.Exceptions;
using C0deGeek.ObjectCompare.Extensions;
using Microsoft.Extensions.Logging;

namespace C0deGeek.ObjectCompare.Comparison.Strategies;

/// <summary>
/// Strategy for comparing simple value types
/// </summary>
public class SimpleTypeComparisonStrategy(ComparisonConfig config) : ComparisonStrategyBase(config)
{
    private static readonly HashSet<Type> SimpleTypes =
    [
        typeof(byte), typeof(sbyte),
        typeof(short), typeof(ushort),
        typeof(int), typeof(uint),
        typeof(long), typeof(ulong),
        typeof(float), typeof(double),
        typeof(decimal), typeof(bool),
        typeof(char), typeof(string),
        typeof(DateTime), typeof(DateTimeOffset),
        typeof(TimeSpan), typeof(Guid)
    ];

    public override bool CanHandle(Type type)
    {
        return type.IsPrimitive || 
               type.IsEnum || 
               SimpleTypes.Contains(type) ||
               Nullable.GetUnderlyingType(type) != null;
    }

    public override int Priority => 100;

    public override bool Compare(object? obj1, object? obj2, string path, 
        ComparisonResult result, ComparisonContext context)
    {
        LogComparison(nameof(SimpleTypeComparisonStrategy), path, obj1, obj2);

        if (HandleNulls(obj1, obj2, path, result)) return result.AreEqual;

        var type = obj1!.GetType();
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        try
        {
            if (Config.CustomComparers.TryGetValue(underlyingType, out var customComparer))
            {
                if (!customComparer.AreEqual(obj1, obj2, Config))
                {
                    result.AddDifference(
                        $"Custom comparison failed for type {underlyingType.Name}", path);
                    result.AreEqual = false;
                }
                return result.AreEqual;
            }

            return CompareValues(obj1, obj2, underlyingType, path, result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error comparing values of type {Type} at path {Path}", 
                underlyingType.Name, path);
            throw new ComparisonException(
                $"Error comparing values of type {underlyingType.Name}", path, ex);
        }
    }

    private bool CompareValues(object obj1, object obj2, Type type, string path, 
        ComparisonResult result)
    {
        if (type == typeof(double))
        {
            return CompareDoubles((double)obj1, (double)obj2, path, result);
        }
        
        if (type == typeof(float))
        {
            return CompareFloats((float)obj1, (float)obj2, path, result);
        }
        
        if (type == typeof(decimal))
        {
            return CompareDecimals((decimal)obj1, (decimal)obj2, path, result);
        }

        if (type.IsEnum)
        {
            return CompareEnums(obj1, obj2, type, path, result);
        }

        if (!obj1.Equals(obj2))
        {
            result.AddDifference(
                $"Values differ: {obj1} != {obj2}", path);
            result.AreEqual = false;
            return false;
        }

        return true;
    }

    private bool CompareDoubles(double value1, double value2, string path, 
        ComparisonResult result)
    {
        if (!value1.AreFloatingPointEqual(value2, Config))
        {
            result.AddDifference(
                $"Double values differ: {value1} != {value2}", path);
            result.AreEqual = false;
            return false;
        }
        return true;
    }

    private bool CompareFloats(float value1, float value2, string path, 
        ComparisonResult result)
    {
        if (!value1.AreFloatingPointEqual(value2, Config))
        {
            result.AddDifference(
                $"Float values differ: {value1} != {value2}", path);
            result.AreEqual = false;
            return false;
        }
        return true;
    }

    private bool CompareDecimals(decimal value1, decimal value2, string path, 
        ComparisonResult result)
    {
        var rounded1 = Math.Round(value1, Config.DecimalPrecision);
        var rounded2 = Math.Round(value2, Config.DecimalPrecision);

        if (rounded1 != rounded2)
        {
            result.AddDifference(
                $"Decimal values differ: {rounded1} != {rounded2}", path);
            result.AreEqual = false;
            return false;
        }
        return true;
    }

    private bool CompareEnums(object obj1, object obj2, Type enumType, string path, 
        ComparisonResult result)
    {
        if (!Equals(obj1, obj2))
        {
            result.AddDifference(
                $"Enum values differ: {obj1} != {obj2}", path);
            result.AreEqual = false;
            return false;
        }
        return true;
    }
}