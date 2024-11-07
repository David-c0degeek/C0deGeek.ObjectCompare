using C0deGeek.ObjectCompare.Common;

namespace C0deGeek.ObjectCompare.ValueObjects;

/// <summary>
/// Provides comparison functionality for value objects with precision settings
/// </summary>
public class PrecisionValueObjectComparer<T>(
    Func<T, T, double, bool> precisionComparison,
    double epsilon = 1e-10)
    where T : ValueObject
{
    private readonly Func<T, T, double, bool> _precisionComparison = Guard.ThrowIfNull(precisionComparison, 
        nameof(precisionComparison));

    public bool AreEqual(T? obj1, T? obj2)
    {
        if (obj1 is null && obj2 is null) return true;
        if (obj1 is null || obj2 is null) return false;

        return _precisionComparison(obj1, obj2, epsilon);
    }

    /// <summary>
    /// Creates a comparer that uses relative error for comparison
    /// </summary>
    public static PrecisionValueObjectComparer<T> CreateRelative(
        double relativeError = 1e-10)
    {
        return new PrecisionValueObjectComparer<T>((obj1, obj2, epsilon) =>
            obj1.EqualsWithTolerance(obj2, epsilon), relativeError);
    }

    /// <summary>
    /// Creates a comparer that uses absolute error for comparison
    /// </summary>
    public static PrecisionValueObjectComparer<T> CreateAbsolute(
        double absoluteError = 1e-10)
    {
        return new PrecisionValueObjectComparer<T>((obj1, obj2, epsilon) =>
                obj1.Equals(obj2) || obj1.EqualsWithTolerance(obj2, epsilon), 
            absoluteError);
    }
}