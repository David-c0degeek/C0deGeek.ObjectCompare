using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for deep comparison with path tracking
/// </summary>
public interface IPathTrackingDeepComparer : IDeepComparer
{
    ComparisonResult CompareDeepWithPath<T>(T? obj1, T? obj2, 
        string path, 
        ComparisonConfig config);
}