using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Base interface for comparison operations
/// </summary>
public interface IComparer<in T>
{
    ComparisonResult Compare(T? obj1, T? obj2);
}