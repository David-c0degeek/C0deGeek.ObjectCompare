namespace C0deGeek.ObjectCompare.Resources;

/// <summary>
/// Provides a scope for automatically managing resource acquisition and release
/// </summary>
public readonly struct ResourceScope<T> : IAsyncDisposable where T : class
{
    private readonly ComparisonResourcePool _pool;
    private readonly T _resource;

    internal ResourceScope(ComparisonResourcePool pool, T resource)
    {
        _pool = pool;
        _resource = resource;
    }

    public T Resource => _resource;

    public async ValueTask DisposeAsync()
    {
        if (_resource is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_resource is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _pool.ReleaseComparer(_resource);
    }
}