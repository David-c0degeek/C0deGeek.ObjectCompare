namespace C0deGeek.ObjectCompare.Resources;

/// <summary>
/// Provides asynchronous locking capabilities
/// </summary>
public sealed class AsyncLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Task<IDisposable> _releaser;

    public AsyncLock()
    {
        _releaser = Task.FromResult<IDisposable>(new Releaser(this));
    }

    public async Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return await _releaser;
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }

    private sealed class Releaser : IDisposable
    {
        private readonly AsyncLock _toRelease;

        internal Releaser(AsyncLock toRelease)
        {
            _toRelease = toRelease;
        }

        public void Dispose()
        {
            _toRelease._semaphore.Release();
        }
    }
}