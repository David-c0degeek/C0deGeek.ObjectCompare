using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for asynchronous comparison operations
/// </summary>
public interface IAsyncComparer<in T>
{
    Task<ComparisonResult> CompareAsync(T? obj1, T? obj2, 
        CancellationToken cancellationToken = default);
}