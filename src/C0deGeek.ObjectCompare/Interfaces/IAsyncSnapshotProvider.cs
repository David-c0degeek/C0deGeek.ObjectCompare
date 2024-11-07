namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for asynchronous snapshot operations
/// </summary>
public interface IAsyncSnapshotProvider
{
    Task<T?> TakeSnapshotAsync<T>(T? obj, 
        CancellationToken cancellationToken = default);
        
    Task<bool> RestoreSnapshotAsync<T>(T target, T snapshot, 
        CancellationToken cancellationToken = default);
}