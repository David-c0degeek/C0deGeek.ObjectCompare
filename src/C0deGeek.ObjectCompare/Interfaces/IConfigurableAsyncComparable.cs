using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for asynchronous comparison with configuration
/// </summary>
public interface IConfigurableAsyncComparable<T>
{
    Task<bool> CompareToAsync(T? other, 
        ComparisonConfig config, 
        CancellationToken cancellationToken = default);
}