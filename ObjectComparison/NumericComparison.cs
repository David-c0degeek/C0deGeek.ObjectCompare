namespace ObjectComparison;

/// <summary>
/// Improved numeric comparison utilities
/// </summary>
internal static class NumericComparison
{
    public static bool AreFloatingPointEqual(double value1, double value2, ComparisonConfig config)
    {
        if (double.IsNaN(value1) && double.IsNaN(value2))
            return true;

        if (double.IsInfinity(value1) || double.IsInfinity(value2))
            return value1 == value2;

        if (config.UseRelativeFloatingPointComparison)
        {
            return AreRelativelyEqual(value1, value2, config.FloatingPointTolerance);
        }

        return Math.Abs(value1 - value2) <= config.FloatingPointTolerance;
    }

    public static bool AreFloatingPointEqual(float value1, float value2, ComparisonConfig config)
    {
        if (float.IsNaN(value1) && float.IsNaN(value2))
            return true;

        if (float.IsInfinity(value1) || float.IsInfinity(value2))
            return value1 == value2;

        if (config.UseRelativeFloatingPointComparison)
        {
            return AreRelativelyEqual(value1, value2, (float)config.FloatingPointTolerance);
        }

        return Math.Abs(value1 - value2) <= config.FloatingPointTolerance;
    }

    private static bool AreRelativelyEqual(double value1, double value2, double relativeTolerance)
    {
        if (value1 == value2)
            return true;

        var absoluteDifference = Math.Abs(value1 - value2);
        var maxValue = Math.Max(Math.Abs(value1), Math.Abs(value2));

        if (maxValue < double.Epsilon)
            return absoluteDifference < double.Epsilon;

        return absoluteDifference / maxValue <= relativeTolerance;
    }

    private static bool AreRelativelyEqual(float value1, float value2, float relativeTolerance)
    {
        if (value1 == value2)
            return true;

        var absoluteDifference = Math.Abs(value1 - value2);
        var maxValue = Math.Max(Math.Abs(value1), Math.Abs(value2));

        if (maxValue < float.Epsilon)
            return absoluteDifference < float.Epsilon;

        return absoluteDifference / maxValue <= relativeTolerance;
    }
}