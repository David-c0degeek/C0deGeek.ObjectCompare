using C0deGeek.ObjectCompare.Common;

namespace C0deGeek.ObjectCompare.ValueObjects;

/// <summary>
/// Base class for implementing value objects with proper equality support
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the components that make up the value object's identity
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public bool Equals(ValueObject? other)
    {
        return Equals((object?)other);
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(17, (current, obj) =>
                current * 23 + (obj?.GetHashCode() ?? 0));
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    /// Creates a shallow copy of the value object
    /// </summary>
    public ValueObject Clone()
    {
        return (ValueObject)MemberwiseClone();
    }

    /// <summary>
    /// Determines if this value object is equal to another within a specified tolerance
    /// </summary>
    public virtual bool EqualsWithTolerance(ValueObject? other, double tolerance)
    {
        return Equals(other);
    }
}

/// <summary>
/// Base class for implementing value objects with generic type constraints
/// </summary>
public abstract class ValueObject<T> : ValueObject where T : ValueObject<T>
{
    public static bool operator <(ValueObject<T> left, ValueObject<T> right)
    {
        return CompareComponents(left, right) < 0;
    }

    public static bool operator >(ValueObject<T> left, ValueObject<T> right)
    {
        return CompareComponents(left, right) > 0;
    }

    public static bool operator <=(ValueObject<T> left, ValueObject<T> right)
    {
        return CompareComponents(left, right) <= 0;
    }

    public static bool operator >=(ValueObject<T> left, ValueObject<T> right)
    {
        return CompareComponents(left, right) >= 0;
    }

    private static int CompareComponents(ValueObject<T> left, ValueObject<T> right)
    {
        Guard.ThrowIfNull(left, nameof(left));
        Guard.ThrowIfNull(right, nameof(right));

        var leftComponents = left.GetEqualityComponents().ToList();
        var rightComponents = right.GetEqualityComponents().ToList();

        var length = Math.Min(leftComponents.Count, rightComponents.Count);

        for (var i = 0; i < length; i++)
        {
            var comparison = CompareComponent(leftComponents[i], rightComponents[i]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return leftComponents.Count.CompareTo(rightComponents.Count);
    }

    private static int CompareComponent(object? left, object? right)
    {
        if (left == null && right == null) return 0;
        if (left == null) return -1;
        if (right == null) return 1;

        if (left is IComparable comparable)
        {
            return comparable.CompareTo(right);
        }

        return left.Equals(right) ? 0 : -1;
    }
}