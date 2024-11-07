using System.Collections.Concurrent;
using C0deGeek.ObjectCompare.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace C0deGeek.ObjectCompare.Resources;

/// <summary>
/// Manages resources and handles resource cleanup for comparison operations
/// </summary>
public sealed class ResourceManager : IDisposable
{
    private readonly ConcurrentDictionary<string, IDisposable> _resources = new();
    private readonly ILogger _logger;
    private readonly ComparisonResourcePool _resourcePool;
    private bool _disposed;

    public ResourceManager(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _resourcePool = new ComparisonResourcePool(logger: _logger);
    }

    public void RegisterResource<T>(string key, T resource) where T : class, IDisposable
    {
        ThrowIfDisposed();
        Guard.ThrowIfNullOrEmpty(key, nameof(key));
        Guard.ThrowIfNull(resource, nameof(resource));

        if (_resources.TryAdd(key, resource))
        {
            _logger.LogDebug("Registered resource {Key} of type {Type}", 
                key, typeof(T).Name);
        }
        else
        {
            _logger.LogWarning("Resource {Key} already exists", key);
            throw new InvalidOperationException($"Resource {key} already exists");
        }
    }

    public bool TryGetResource<T>(string key, out T? resource) where T : class, IDisposable
    {
        ThrowIfDisposed();
        resource = null;

        if (!_resources.TryGetValue(key, out var disposable))
            return false;

        if (disposable is not T typedResource)
        {
            _logger.LogWarning(
                "Resource {Key} is of type {ActualType} but {RequestedType} was requested",
                key, disposable.GetType().Name, typeof(T).Name);
            return false;
        }

        resource = typedResource;
        return true;
    }

    public async Task<T> GetOrCreateResourceAsync<T>(string key, 
        Func<Task<T>> factory) where T : class, IDisposable
    {
        ThrowIfDisposed();

        if (TryGetResource<T>(key, out var existing))
            return existing;

        var resource = await factory();
        RegisterResource(key, resource);
        return resource;
    }

    public bool ReleaseResource(string key)
    {
        if (_disposed) return false;

        if (_resources.TryRemove(key, out var resource))
        {
            try
            {
                resource.Dispose();
                _logger.LogDebug("Released resource {Key}", key);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing resource {Key}", key);
                return false;
            }
        }

        return false;
    }

    public void Clear()
    {
        if (_disposed) return;

        foreach (var key in _resources.Keys.ToList())
        {
            ReleaseResource(key);
        }
    }

    public async Task<ResourceScope<T>> AcquireResourceScopeAsync<T>() 
        where T : class, IDisposable
    {
        return await _resourcePool.AcquireResourceScopeAsync<T>();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ResourceManager));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Clear();
        _resourcePool.Dispose();
        _disposed = true;
    }

    public static ResourceManager Create(Action<ResourceManagerOptions> configure)
    {
        var options = new ResourceManagerOptions();
        configure(options);
        
        return new ResourceManager(options.Logger);
    }
}