using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Interfaces;

namespace C0deGeek.ObjectCompare.Numeric;

/// <summary>
/// Provides specialized comparison for floating-point values
/// </summary>
public class FloatingPointComparer : ICustomComparer
{
    private readonly double _tolerance;
    private readonly bool _useRelativeComparison;

    public FloatingPointComparer(double tolerance = 1e-10, bool useRelativeComparison = true)
    {
        _tolerance = Guard.ThrowIfLessThan(tolerance, 0.0, nameof(tolerance));
        _useRelativeComparison = useRelativeComparison;
    }

    public bool AreEqual(object? obj1, object? obj2, ComparisonConfig config)
    {
        return (obj1, obj2) switch
        {
            (double d1, double d2) => AreDoubleValuesEqual(d1, d2),
            (float f1, float f2) => AreFloatValuesEqual(f1, f2),
            _ => false
        };
    }

    private bool AreDoubleValuesEqual(double value1, double value2)
    {
        if (double.IsNaN(value1) && double.IsNaN(value2))
            return true;

        if (double.IsInfinity(value1) || double.IsInfinity(value2))
            return value1.Equals(value2);

        return _useRelativeComparison
            ? AreRelativelyEqual(value1, value2, _tolerance)
            : Math.Abs(value1 - value2) <= _tolerance;
    }

    private bool AreFloatValuesEqual(float value1, float value2)
    {
        if (float.IsNaN(value1) && float.IsNaN(value2))
            return true;

        if (float.IsInfinity(value1) || float.IsInfinity(value2))
            return value1.Equals(value2);

        return _useRelativeComparison
            ? AreRelativelyEqual(value1, value2, (float)_tolerance)
            : Math.Abs(value1 - value2) <= _tolerance;
    }

    private static bool AreRelativelyEqual(double value1, double value2, double relativeTolerance)
    {
        if (value1.Equals(value2))
            return true;

        var absoluteDifference = Math.Abs(value1 - value2);
        var maxValue = Math.Max(Math.Abs(value1), Math.Abs(value2));

        if (maxValue < double.Epsilon)
            return absoluteDifference < double.Epsilon;

        return absoluteDifference / maxValue <= relativeTolerance;
    }

    private static bool AreRelativelyEqual(float value1, float value2, float relativeTolerance)
    {
        if (value1.Equals(value2))
            return true;

        var absoluteDifference = Math.Abs(value1 - value2);
        var maxValue = Math.Max(Math.Abs(value1), Math.Abs(value2));

        if (maxValue < float.Epsilon)
            return absoluteDifference < float.Epsilon;

        return absoluteDifference / maxValue <= relativeTolerance;
    }

    /// <summary>
    /// Creates a comparer that uses Units in Last Place (ULP) for comparison
    /// </summary>
    public static FloatingPointComparer CreateUlpComparer(int maxUlps = 4)
    {
        Guard.ThrowIfLessThan(maxUlps, 1, nameof(maxUlps));
        var tolerance = Math.Pow(2, -52) * maxUlps; // For double precision
        return new FloatingPointComparer(tolerance, false);
    }
}