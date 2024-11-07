using C0deGeek.ObjectCompare.Cloning;
using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Comparison.Exceptions;
using C0deGeek.ObjectCompare.Comparison.Strategies;
using C0deGeek.ObjectCompare.Enums;
using C0deGeek.ObjectCompare.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
namespace C0deGeek.ObjectCompare.Comparison.Base;

/// <summary>
/// Main object comparer class with optimized implementation
/// </summary>
public class ObjectComparer : IDisposable
{
    private readonly ComparisonConfig _config;
    private readonly ExpressionCloner _cloner;
    private readonly ILogger _logger;
    private readonly Dictionary<Type, IComparisonStrategy> _strategies;
    private bool _disposed;

    public ObjectComparer(ComparisonConfig? config = null)
    {
        _config = config ?? new ComparisonConfig();
        _cloner = new ExpressionCloner(_config);
        _logger = _config.Logger ?? NullLogger.Instance;
        _strategies = InitializeStrategies();
    }

    private Dictionary<Type, IComparisonStrategy> InitializeStrategies()
    {
        return new Dictionary<Type, IComparisonStrategy>
        {
            { typeof(ValueType), new SimpleTypeComparisonStrategy(_config) },
            { typeof(IEnumerable), new CollectionComparisonStrategy(_config) },
            { typeof(object), new ComplexTypeComparisonStrategy(_config) }
        };
    }

    public T? TakeSnapshot<T>(T? obj)
    {
        ThrowIfDisposed();
        _logger.LogDebug("Taking snapshot of {Type}", typeof(T).Name);
        
        if (obj == null) return default;
        
        var type = typeof(T);
    
        // Special handling for arrays
        if (type.IsArray)
        {
            var array = (Array)(object)obj;
            var elementType = type.GetElementType()!;
            var clone = Array.CreateInstance(elementType, array.Length);
        
            Array.Copy(array, clone, array.Length);
            return (T)(object)clone;
        }
        
        try
        {
            return _cloner.Clone(obj);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to take snapshot of {Type}", typeof(T).Name);
            throw;
        }
    }

    public ComparisonResult Compare<T>(T? obj1, T? obj2)
    {
        ThrowIfDisposed();
        _logger.LogDebug("Starting comparison of {Type}", typeof(T).Name);

        var context = new ComparisonContext();
        var result = new ComparisonResult();

        try
        {
            context.Timer.Start();
            CompareObjectsIterative(obj1, obj2, "", result, context);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comparison failed for {Type}", typeof(T).Name);
            throw new ComparisonException("Comparison failed", "", ex);
        }
        finally
        {
            context.Timer.Stop();
            UpdateResultMetrics(result, context);
            LogComparisonMetrics(result);
        }
    }

    private void CompareObjectsIterative(object? obj1, object? obj2, string path,
        ComparisonResult result, ComparisonContext context)
    {
        var stack = new Stack<ComparisonWorkItem>();
        stack.Push(new ComparisonWorkItem(obj1, obj2, path, 0));

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            ProcessComparisonItem(current, stack, result, context);
        }
    }

    private void ProcessComparisonItem(ComparisonWorkItem item, Stack<ComparisonWorkItem> stack,
        ComparisonResult result, ComparisonContext context)
    {
        if (item.Depth >= _config.MaxDepth)
        {
            HandleMaxDepthExceeded(item, result);
            return;
        }

        if (HandleNulls(item.Obj1, item.Obj2, item.Path, result))
        {
            return;
        }

        var type = item.Obj1!.GetType();

        context.PushObject(item.Obj1);

        try
        {
            var strategy = GetComparisonStrategy(type);
            if (!strategy.Compare(item.Obj1, item.Obj2!, item.Path, result, context))
            {
                result.AreEqual = false;
            }
        }
        finally
        {
            context.PopObject();
        }
    }

    private IComparisonStrategy GetComparisonStrategy(Type type)
    {
        foreach (var kvp in _strategies)
        {
            if (kvp.Key.IsAssignableFrom(type))
            {
                return kvp.Value;
            }
        }

        return _strategies[typeof(object)];
    }

    private bool HandleNulls(object? obj1, object? obj2, string path, ComparisonResult result)
    {
        if (ReferenceEquals(obj1, obj2)) return true;

        if (obj1 is null || obj2 is null)
        {
            if (_config.NullValueHandling == NullHandling.Loose && IsEmpty(obj1) && IsEmpty(obj2))
            {
                return true;
            }

            result.Differences.Add($"Null difference at {path}: one object is null while the other is not");
            result.AreEqual = false;
            return true;
        }

        return false;
    }

    private static bool IsEmpty(object? obj)
    {
        return obj switch
        {
            null => true,
            string str => string.IsNullOrEmpty(str),
            IEnumerable enumerable => !enumerable.Cast<object>().Any(),
            _ => false
        };
    }

    private void HandleMaxDepthExceeded(ComparisonWorkItem item, ComparisonResult result)
    {
        result.MaxDepthPath = item.Path;
        result.MaxDepthReached = item.Depth;
        result.AreEqual = false;
        result.Differences.Add(ExceptionHelper.CreateMaxDepthMessage(_config.MaxDepth, item.Path));
    }

    private void UpdateResultMetrics(ComparisonResult result, ComparisonContext context)
    {
        result.ComparisonTime = context.Timer.Elapsed;
        result.ObjectsCompared = context.ObjectsCompared;
        result.MaxDepthReached = context.MaxDepthReached;
    }

    private void LogComparisonMetrics(ComparisonResult result)
    {
        _logger.LogInformation(
            "Comparison completed in {Time}ms. Objects compared: {Objects}, Max depth: {Depth}, Differences: {Differences}",
            result.ComparisonTime.TotalMilliseconds,
            result.ObjectsCompared,
            result.MaxDepthReached,
            result.Differences.Count);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ObjectComparer));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}