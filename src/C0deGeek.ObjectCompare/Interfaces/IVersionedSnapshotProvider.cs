namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for versioned snapshot operations
/// </summary>
public interface IVersionedSnapshotProvider : ISnapshotProvider
{
    T? TakeSnapshot<T>(T? obj, string version);
    bool RestoreSnapshot<T>(T target, string version);
    void PruneSnapshots<T>(TimeSpan olderThan);
}