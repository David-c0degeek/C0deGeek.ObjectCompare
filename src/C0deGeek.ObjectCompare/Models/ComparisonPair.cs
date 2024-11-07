using System.Runtime.CompilerServices;
using C0deGeek.ObjectCompare.Common;

namespace C0deGeek.ObjectCompare.Models;

public readonly struct ComparisonPair(object obj1, object obj2) : IEquatable<ComparisonPair>
{
    private readonly object _obj1 = Guard.ThrowIfNull(obj1, nameof(obj1));
    private readonly object _obj2 = Guard.ThrowIfNull(obj2, nameof(obj2));
    private readonly int _hashCode = HashCode.Combine(
        RuntimeHelpers.GetHashCode(obj1),
        RuntimeHelpers.GetHashCode(obj2)
    );

    public bool Equals(ComparisonPair other)
    {
        return ReferenceEquals(_obj1, other._obj1) &&
               ReferenceEquals(_obj2, other._obj2);
    }

    public override bool Equals(object? obj)
    {
        return obj is ComparisonPair other && Equals(other);
    }

    public override int GetHashCode() => _hashCode;

    public static bool operator ==(ComparisonPair left, ComparisonPair right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ComparisonPair left, ComparisonPair right)
    {
        return !left.Equals(right);
    }
}