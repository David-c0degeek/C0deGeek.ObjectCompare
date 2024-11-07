namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for asynchronous comparison with progress reporting
/// </summary>
public interface IProgressReportingAsyncComparable<T>
{
    Task<bool> CompareToAsync(T? other, 
        IProgress<ComparisonProgress> progress, 
        CancellationToken cancellationToken = default);
}