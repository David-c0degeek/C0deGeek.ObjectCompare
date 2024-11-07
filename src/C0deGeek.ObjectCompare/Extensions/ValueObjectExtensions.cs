using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Configuration;
using C0deGeek.ObjectCompare.ValueObjects;

namespace C0deGeek.ObjectCompare.Extensions;

/// <summary>
/// Provides extension methods for value objects
/// </summary>
public static class ValueObjectExtensions
{
    public static bool EqualsWithTolerance<T>(this T obj1, T? obj2, 
        double tolerance) where T : ValueObject
    {
        if (obj2 == null) return false;
        return obj1.EqualsWithTolerance(obj2, tolerance);
    }

    public static bool EqualsRelative<T>(this T obj1, T? obj2, 
        double relativeTolerance) where T : ValueObject
    {
        if (obj2 == null) return false;

        var components1 = obj1.GetType()
            .GetProperties()
            .Where(p => p.CanRead)
            .Select(p => p.GetValue(obj1))
            .OfType<IComparable>();

        var components2 = obj2.GetType()
            .GetProperties()
            .Where(p => p.CanRead)
            .Select(p => p.GetValue(obj2))
            .OfType<IComparable>();

        return components1.Zip(components2, (v1, v2) => 
            AreRelativelyEqual(v1, v2, relativeTolerance)).All(x => x);
    }

    public static bool EqualsExact<T>(this T obj1, T? obj2) where T : ValueObject
    {
        if (obj2 == null) return false;
        return obj1.Equals(obj2);
    }

    public static T Clone<T>(this T obj) where T : ValueObject
    {
        return (T)obj.Clone();
    }

    public static bool IsEquivalentTo<T>(this T obj1, T? obj2, 
        Action<ValueObjectConfiguration>? configure = null) where T : ValueObject
    {
        if (obj2 == null) return false;

        var config = new ValueObjectConfiguration();
        configure?.Invoke(config);

        var comparer = new ValueObjectComparer();
        return comparer.AreEqual(obj1, obj2, new ComparisonConfig());
    }

    private static bool AreRelativelyEqual(IComparable value1, IComparable value2, 
        double relativeTolerance)
    {
        if (value1 is double d1 && value2 is double d2)
        {
            return AreRelativelyEqualDoubles(d1, d2, relativeTolerance);
        }

        if (value1 is decimal m1 && value2 is decimal m2)
        {
            return AreRelativelyEqualDecimals(m1, m2, (decimal)relativeTolerance);
        }

        return value1.Equals(value2);
    }

    private static bool AreRelativelyEqualDoubles(double value1, double value2, 
        double relativeTolerance)
    {
        if (value1.Equals(value2)) return true;

        var absoluteDifference = Math.Abs(value1 - value2);
        var maxValue = Math.Max(Math.Abs(value1), Math.Abs(value2));

        if (maxValue < double.Epsilon)
            return absoluteDifference < double.Epsilon;

        return absoluteDifference / maxValue <= relativeTolerance;
    }

    private static bool AreRelativelyEqualDecimals(decimal value1, decimal value2, 
        decimal relativeTolerance)
    {
        if (value1 == value2) return true;

        var absoluteDifference = Math.Abs(value1 - value2);
        var maxValue = Math.Max(Math.Abs(value1), Math.Abs(value2));

        if (maxValue == 0m)
            return absoluteDifference == 0m;

        return absoluteDifference / maxValue <= relativeTolerance;
    }
}