namespace C0deGeek.ObjectCompare.Common;

/// <summary>
/// Provides thread-safe locking mechanisms using IDisposable pattern
/// </summary>
internal static class LockUtilities
{
    /// <summary>
    /// Provides a disposable write lock scope
    /// </summary>
    public sealed class WriteLockScope : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;
        private bool _disposed;

        public WriteLockScope(ReaderWriterLockSlim @lock)
        {
            _lock = Guard.ThrowIfNull(@lock, nameof(@lock));
            _lock.EnterWriteLock();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _lock.ExitWriteLock();
            _disposed = true;
        }
    }

    /// <summary>
    /// Provides a disposable read lock scope
    /// </summary>
    public sealed class ReadLockScope : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;
        private bool _disposed;

        public ReadLockScope(ReaderWriterLockSlim @lock)
        {
            _lock = Guard.ThrowIfNull(@lock, nameof(@lock));
            _lock.EnterReadLock();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _lock.ExitReadLock();
            _disposed = true;
        }
    }

    /// <summary>
    /// Provides a disposable upgradeable read lock scope
    /// </summary>
    public sealed class UpgradeableReadLockScope : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;
        private bool _disposed;

        public UpgradeableReadLockScope(ReaderWriterLockSlim @lock)
        {
            _lock = Guard.ThrowIfNull(@lock, nameof(@lock));
            _lock.EnterUpgradeableReadLock();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _lock.ExitUpgradeableReadLock();
            _disposed = true;
        }
    }

    /// <summary>
    /// Creates a write lock scope
    /// </summary>
    public static WriteLockScope CreateWriteLockScope(ReaderWriterLockSlim @lock)
    {
        return new WriteLockScope(@lock);
    }

    /// <summary>
    /// Creates a read lock scope
    /// </summary>
    public static ReadLockScope CreateReadLockScope(ReaderWriterLockSlim @lock)
    {
        return new ReadLockScope(@lock);
    }

    /// <summary>
    /// Creates an upgradeable read lock scope
    /// </summary>
    public static UpgradeableReadLockScope CreateUpgradeableReadLockScope(ReaderWriterLockSlim @lock)
    {
        return new UpgradeableReadLockScope(@lock);
    }
}