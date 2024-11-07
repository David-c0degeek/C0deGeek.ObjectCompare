namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for precise value comparison
/// </summary>
public interface IPrecisionComparer<in T>
{
    bool AreEqual(T value1, T value2, int precision);
    bool AreEqualWithinTolerance(T value1, T value2, double tolerance);
    bool AreEqualRelative(T value1, T value2, double relativeTolerance);
}