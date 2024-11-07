using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace C0deGeek.ObjectCompare.Comparison.Strategies;

/// <summary>
/// Base class for comparison strategies providing common functionality
/// </summary>
public abstract class ComparisonStrategyBase(ComparisonConfig config) : IComparisonStrategy
{
    protected readonly ComparisonConfig Config = Guard.ThrowIfNull(config, nameof(config));
    protected readonly ILogger Logger = config.Logger ?? NullLogger.Instance;

    public abstract bool CanHandle(Type type);
    
    public abstract bool Compare(object? obj1, object? obj2, string path, 
        ComparisonResult result, ComparisonContext context);
    
    public abstract int Priority { get; }

    protected bool HandleNulls(object? obj1, object? obj2, string path, ComparisonResult result)
    {
        if (ReferenceEquals(obj1, obj2)) return true;

        if (obj1 is null || obj2 is null)
        {
            if (Config.NullValueHandling == NullHandling.Loose && IsEmpty(obj1) && IsEmpty(obj2))
            {
                return true;
            }

            result.AddDifference(
                $"Null difference: one object is null while the other is not", path);
            result.AreEqual = false;
            return true;
        }

        return false;
    }

    protected static bool IsEmpty(object? obj)
    {
        return obj switch
        {
            null => true,
            string str => string.IsNullOrEmpty(str),
            IEnumerable enumerable => !enumerable.Cast<object>().Any(),
            _ => false
        };
    }

    protected void LogComparison(string strategyName, string path, object? obj1, object? obj2)
    {
        Logger.LogDebug(
            "Using {Strategy} to compare objects at path {Path}. Types: {Type1}, {Type2}",
            strategyName,
            path,
            obj1?.GetType().Name ?? "null",
            obj2?.GetType().Name ?? "null");
    }
}