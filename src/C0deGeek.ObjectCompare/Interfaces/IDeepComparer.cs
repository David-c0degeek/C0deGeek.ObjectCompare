using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for deep comparison operations
/// </summary>
public interface IDeepComparer
{
    ComparisonResult CompareDeep<T>(T? obj1, T? obj2, int maxDepth);
    ComparisonResult CompareDeep<T>(T? obj1, T? obj2, ComparisonConfig config);
}