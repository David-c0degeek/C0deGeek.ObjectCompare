using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Interfaces;

namespace C0deGeek.ObjectCompare.Numeric;

/// <summary>
/// Provides specialized comparison for decimal values with configurable precision
/// </summary>
public class DecimalComparer(int precision = 4) : ICustomComparer
{
    private readonly int _precision = Guard.ThrowIfLessThan(precision, 0, nameof(precision));

    public bool AreEqual(object obj1, object obj2, ComparisonConfig config)
    {
        if (obj1 is not decimal dec1 || obj2 is not decimal dec2)
        {
            return false;
        }

        var rounded1 = Math.Round(dec1, _precision);
        var rounded2 = Math.Round(dec2, _precision);

        return rounded1 == rounded2;
    }

    /// <summary>
    /// Determines if two decimal values are equivalent within a specified epsilon
    /// </summary>
    public static bool AreEquivalent(decimal value1, decimal value2, decimal epsilon)
    {
        return Math.Abs(value1 - value2) <= epsilon;
    }

    /// <summary>
    /// Determines if two decimal values are equivalent as percentages
    /// </summary>
    public static bool ArePercentagesEquivalent(decimal value1, decimal value2, decimal maxPercentageDifference)
    {
        if (value1 == value2) return true;
        
        var larger = Math.Max(Math.Abs(value1), Math.Abs(value2));
        if (larger == 0m) return true;

        var percentageDifference = Math.Abs(value1 - value2) / larger * 100m;
        return percentageDifference <= maxPercentageDifference;
    }
}