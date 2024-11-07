using C0deGeek.ObjectCompare.Common;
using Microsoft.Extensions.Logging;

namespace C0deGeek.ObjectCompare.Resources;

/// <summary>
/// Base class for disposable comparison components
/// </summary>
public abstract class DisposableComparisonComponent(ILogger logger) : IDisposable, IAsyncDisposable
{
    private bool _disposed;
    protected readonly ILogger Logger = Guard.ThrowIfNull(logger, nameof(logger));
    private readonly CancellationTokenSource _cts = new();
    private readonly List<IDisposable> _disposables = [];
    private readonly AsyncLock _lock = new();

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            try
            {
                _cts.Cancel();
                _cts.Dispose();

                foreach (var disposable in _disposables)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error disposing resource");
                    }
                }

                _disposables.Clear();
                DisposeResources();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during disposal");
            }
            finally
            {
                _lock.Dispose();
            }
        }

        _disposed = true;
    }

    protected abstract void DisposeResources();

    protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    protected void RegisterForDisposal(IDisposable disposable)
    {
        ThrowIfDisposed();
        _disposables.Add(disposable);
    }

    protected CancellationToken CancellationToken => _cts.Token;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await DisposeAsyncCore();

        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        foreach (var disposable in _disposables)
        {
            if (disposable is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                disposable.Dispose();
            }
        }
    }

    protected async Task<TResult> ExecuteWithLockAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        using (await _lock.LockAsync(cancellationToken))
        {
            return await operation();
        }
    }

    protected async Task ExecuteWithLockAsync(
        Func<Task> operation,
        CancellationToken cancellationToken = default)
    {
        using (await _lock.LockAsync(cancellationToken))
        {
            await operation();
        }
    }
}