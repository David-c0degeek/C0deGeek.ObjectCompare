using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for collection comparison with element matching
/// </summary>
public interface IMatchingCollectionComparer : ICollectionComparer
{
    bool CompareWithMatching(IEnumerable collection1, IEnumerable collection2, 
        string path, ComparisonResult result, 
        Func<object, object, bool> matchingPredicate);
}