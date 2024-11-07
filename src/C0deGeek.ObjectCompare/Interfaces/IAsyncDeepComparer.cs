using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for asynchronous deep comparison operations
/// </summary>
public interface IAsyncDeepComparer
{
    Task<ComparisonResult> CompareDeepAsync<T>(T? obj1, T? obj2, 
        int maxDepth, 
        CancellationToken cancellationToken = default);
        
    Task<ComparisonResult> CompareDeepAsync<T>(T? obj1, T? obj2, 
        ComparisonConfig config, 
        CancellationToken cancellationToken = default);
}