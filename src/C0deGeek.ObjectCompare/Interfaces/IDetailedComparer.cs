using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for comparison with detailed difference tracking
/// </summary>
public interface IDetailedComparer<in T>
{
    ComparisonResult Compare(T? obj1, T? obj2, string path);
    IEnumerable<string> GetDifferences(T? obj1, T? obj2);
}