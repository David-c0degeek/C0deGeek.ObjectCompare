using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for ordered collection comparison
/// </summary>
public interface IOrderedCollectionComparer : ICollectionComparer
{
    bool CompareOrdered(IEnumerable collection1, IEnumerable collection2, 
        string path, ComparisonResult result);
}