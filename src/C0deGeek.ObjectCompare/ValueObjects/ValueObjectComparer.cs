using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.ValueObjects;

/// <summary>
/// Provides comparison functionality for value objects
/// </summary>
public class ValueObjectComparer(double tolerance = 1e-10)
{
    private readonly double? _tolerance = tolerance;

    public bool AreEqual(object obj1, object obj2, ComparisonConfig config)
    {
        if (obj1 is not ValueObject valueObj1 || obj2 is not ValueObject valueObj2)
        {
            return false;
        }

        if (_tolerance.HasValue)
        {
            return valueObj1.EqualsWithTolerance(valueObj2, _tolerance.Value);
        }

        return valueObj1.Equals(valueObj2);
    }

    /// <summary>
    /// Creates a comparer for numeric value objects with specified precision
    /// </summary>
    public static ValueObjectComparer CreateNumericComparer(double precision)
    {
        return new ValueObjectComparer(precision);
    }

    /// <summary>
    /// Creates a comparer that uses the value object's native equality
    /// </summary>
    public static ValueObjectComparer CreateExactComparer()
    {
        return new ValueObjectComparer();
    }
}