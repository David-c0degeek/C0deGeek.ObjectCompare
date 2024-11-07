using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Numeric;

namespace C0deGeek.ObjectCompare.Extensions;

/// <summary>
/// Static extension methods for floating point comparisons
/// </summary>
public static class FloatingPointExtensions
{
    public static bool AreFloatingPointEqual(this double value1, double value2, ComparisonConfig config)
    {
        var comparer = new FloatingPointComparer(
            config.FloatingPointTolerance, 
            config.UseRelativeFloatingPointComparison);
        return comparer.AreEqual(value1, value2, config);
    }

    public static bool AreFloatingPointEqual(this float value1, float value2, ComparisonConfig config)
    {
        var comparer = new FloatingPointComparer(
            config.FloatingPointTolerance, 
            config.UseRelativeFloatingPointComparison);
        return comparer.AreEqual(value1, value2, config);
    }
}