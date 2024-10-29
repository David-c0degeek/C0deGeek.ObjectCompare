using System.Collections.Concurrent;
using System.Dynamic;

namespace ObjectComparison;

/// <summary>
/// Dynamic object handling support
/// </summary>
internal class DynamicObjectComparer
{
    private readonly ComparisonConfig _config;
    private readonly ConcurrentDictionary<Type, IDynamicTypeHandler> _typeHandlers;

    public DynamicObjectComparer(ComparisonConfig config)
    {
        _config = config;
        _typeHandlers = new ConcurrentDictionary<Type, IDynamicTypeHandler>();
        InitializeTypeHandlers();
    }

    private void InitializeTypeHandlers()
    {
        _typeHandlers[typeof(ExpandoObject)] = new ExpandoObjectHandler();
        _typeHandlers[typeof(DynamicObject)] = new DynamicObjectHandler();
        // Add other dynamic type handlers as needed
    }

    public bool AreEqual(object obj1, object obj2, string path, ComparisonResult result)
    {
        var type = obj1?.GetType() ?? obj2?.GetType();
        var handler = GetTypeHandler(type);

        if (handler != null) return handler.Compare(obj1, obj2, path, result, _config);
        
        result.Differences.Add($"Unsupported dynamic type at {path}: {type?.Name}");
        return false;

    }

    private IDynamicTypeHandler GetTypeHandler(Type type)
    {
        if (type == null) return null;

        var (handler, _) = _typeHandlers.GetOrAddWithStatus(type, t =>
        {
            if (typeof(ExpandoObject).IsAssignableFrom(t))
                return new ExpandoObjectHandler();
            if (typeof(DynamicObject).IsAssignableFrom(t))
                return new DynamicObjectHandler();
            // Add other type handler mappings
            return null;
        });

        return handler;
    }
}