using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for custom type comparison
/// </summary>
public interface ICustomTypeComparer
{
    bool CanCompare(Type type);
    bool Compare(object? obj1, object? obj2, ComparisonConfig config);
}