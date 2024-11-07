namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for object snapshotting capabilities
/// </summary>
public interface ISnapshotProvider
{
    T? TakeSnapshot<T>(T? obj);
    bool RestoreSnapshot<T>(T target, T snapshot);
}