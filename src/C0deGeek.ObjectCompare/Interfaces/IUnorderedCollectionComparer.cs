using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for unordered collection comparison
/// </summary>
public interface IUnorderedCollectionComparer : ICollectionComparer
{
    bool CompareUnordered(IEnumerable collection1, IEnumerable collection2, 
        string path, ComparisonResult result);
}