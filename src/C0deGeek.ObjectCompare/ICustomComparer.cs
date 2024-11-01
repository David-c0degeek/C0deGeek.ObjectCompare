namespace C0deGeek.ObjectCompare;

/// <summary>
/// Interface for custom comparison logic
/// </summary>
public interface ICustomComparer
{
    bool AreEqual(object obj1, object obj2, ComparisonConfig config);
}