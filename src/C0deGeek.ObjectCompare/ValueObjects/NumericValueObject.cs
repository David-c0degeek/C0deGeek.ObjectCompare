namespace C0deGeek.ObjectCompare.ValueObjects;

/// <summary>
/// Base class for value objects that have explicit numeric components
/// </summary>
public abstract class NumericValueObject(double epsilon = 1e-10) : ValueObject
{
    /// <summary>
    /// Gets the numeric components that make up the value object's identity
    /// </summary>
    protected abstract IEnumerable<double> GetNumericComponents();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        return GetNumericComponents().Cast<object>();
    }

    public override bool EqualsWithTolerance(ValueObject? other, double tolerance)
    {
        if (other == null || other.GetType() != GetType())
        {
            return false;
        }

        var otherNumeric = (NumericValueObject)other;
        return GetNumericComponents()
            .Zip(otherNumeric.GetNumericComponents(),
                (a, b) => Math.Abs(a - b) <= tolerance)
            .All(x => x);
    }

    public bool EqualsWithinEpsilon(NumericValueObject? other)
    {
        return EqualsWithTolerance(other, epsilon);
    }
}