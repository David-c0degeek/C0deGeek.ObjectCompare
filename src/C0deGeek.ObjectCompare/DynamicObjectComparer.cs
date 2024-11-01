using System.Collections.Concurrent;
using System.Dynamic;

namespace C0deGeek.ObjectCompare;

/// <summary>
/// Dynamic object handling support
/// </summary>
internal sealed class DynamicObjectComparer
{
    private readonly ComparisonConfig _config;
    private readonly ConcurrentDictionary<Type, IDynamicTypeHandler> _typeHandlers;

    public DynamicObjectComparer(ComparisonConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _typeHandlers = new ConcurrentDictionary<Type, IDynamicTypeHandler>();
        InitializeTypeHandlers();
    }

    private void InitializeTypeHandlers()
    {
        _typeHandlers[typeof(ExpandoObject)] = new ExpandoObjectHandler();
        _typeHandlers[typeof(DynamicObject)] = new DynamicObjectHandler();
    }

    public bool AreEqual(object? obj1, object? obj2, string path, ComparisonResult result)
    {
        if (obj1 is null && obj2 is null) return true;
        if (obj1 is null || obj2 is null)
        {
            result.Differences.Add($"One object is null at {path}");
            return false;
        }

        var type = obj1.GetType();
        if (type != obj2.GetType())
        {
            result.Differences.Add($"Type mismatch at {path}: {type.Name} != {obj2.GetType().Name}");
            return false;
        }

        var handler = GetTypeHandler(type);
        if (handler is not null) 
            return handler.Compare(obj1, obj2, path, result, _config);
        
        result.Differences.Add($"Unsupported dynamic type at {path}: {type.Name}");
        return false;

    }

    private IDynamicTypeHandler? GetTypeHandler(Type? type)
    {
        if (type is null) return null;

        return _typeHandlers.GetOrAdd(type, t =>
        {
            if (typeof(ExpandoObject).IsAssignableFrom(t))
                return new ExpandoObjectHandler();
            if (typeof(DynamicObject).IsAssignableFrom(t))
                return new DynamicObjectHandler();
            return NullDynamicTypeHandler.Instance;
        });
    }

    // Null Object Pattern implementation
    private sealed class NullDynamicTypeHandler : IDynamicTypeHandler
    {
        public static readonly NullDynamicTypeHandler Instance = new();
        private NullDynamicTypeHandler() { }

        public bool Compare(object obj1, object obj2, string path, ComparisonResult result, ComparisonConfig config)
        {
            return false;
        }
    }
}