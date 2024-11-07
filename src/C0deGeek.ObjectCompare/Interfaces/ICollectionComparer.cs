using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for collection comparison
/// </summary>
public interface ICollectionComparer
{
    bool CompareCollections(IEnumerable collection1, IEnumerable collection2, string path, ComparisonResult result);
}