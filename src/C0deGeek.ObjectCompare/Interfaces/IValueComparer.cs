using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for value type comparison
/// </summary>
public interface IValueComparer<in T> where T : struct
{
    bool AreEqual(T value1, T value2, ComparisonConfig config);
    bool AreEquivalent(T value1, T value2, double tolerance);
}