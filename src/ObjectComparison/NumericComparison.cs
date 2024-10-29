namespace ObjectComparison;

/// <summary>
/// Improved numeric comparison utilities
/// </summary>
internal static class NumericComparison
{
    private const double DefaultEpsilon = 1e-10;

    public static bool AreFloatingPointEqual(double value1, double value2, ComparisonConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (double.IsNaN(value1) && double.IsNaN(value2))
            return true;

        if (double.IsInfinity(value1) || double.IsInfinity(value2))
            return value1.Equals(value2);

        return config.UseRelativeFloatingPointComparison
            ? AreRelativelyEqual(value1, value2, config.FloatingPointTolerance)
            : Math.Abs(value1 - value2) <= config.FloatingPointTolerance;
    }

    public static bool AreFloatingPointEqual(float value1, float value2, ComparisonConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (float.IsNaN(value1) && float.IsNaN(value2))
            return true;

        if (float.IsInfinity(value1) || float.IsInfinity(value2))
            return value1.Equals(value2);

        return config.UseRelativeFloatingPointComparison
            ? AreRelativelyEqual(value1, value2, (float)config.FloatingPointTolerance)
            : Math.Abs(value1 - value2) <= config.FloatingPointTolerance;
    }

    public static bool AreDecimalsEqual(decimal value1, decimal value2, ComparisonConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        
        var rounded1 = decimal.Round(value1, config.DecimalPrecision);
        var rounded2 = decimal.Round(value2, config.DecimalPrecision);
        return rounded1 == rounded2;
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
}
