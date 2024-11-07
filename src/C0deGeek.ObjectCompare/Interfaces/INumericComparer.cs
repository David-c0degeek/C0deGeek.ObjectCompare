namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for numeric value comparison
/// </summary>
public interface INumericComparer<in T> where T : struct, IComparable<T>
{
    bool AreEqual(T value1, T value2, double tolerance);
    bool AreEquivalent(T value1, T value2, double relativeTolerance);
    int Compare(T value1, T value2);
}