using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for comparison with configuration
/// </summary>
public interface IConfigurableComparer<in T>
{
    ComparisonResult Compare(T? obj1, T? obj2, ComparisonConfig config);
}