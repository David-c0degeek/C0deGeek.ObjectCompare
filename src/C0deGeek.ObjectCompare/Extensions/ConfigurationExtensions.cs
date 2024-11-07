using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Enums;
using C0deGeek.ObjectCompare.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace C0deGeek.ObjectCompare.Extensions;

/// <summary>
/// Provides extension methods for configuration objects
/// </summary>
public static class ConfigurationExtensions
{
    public static ComparisonConfig WithMaxDepth(this ComparisonConfig config, 
        int maxDepth)
    {
        config.MaxDepth = maxDepth;
        return config;
    }

    public static ComparisonConfig WithTimeout(this ComparisonConfig config, 
        TimeSpan timeout)
    {
        config.ComparisonTimeout = timeout;
        return config;
    }

    public static ComparisonConfig IgnoreProperty(this ComparisonConfig config, 
        string propertyName)
    {
        config.ExcludedProperties.Add(propertyName);
        return config;
    }

    public static ComparisonConfig WithCustomComparer<T>(
        this ComparisonConfig config, 
        ICustomComparer comparer)
    {
        config.CustomComparers[typeof(T)] = comparer;
        return config;
    }

    public static ComparisonConfig WithCollectionComparer<T>(
        this ComparisonConfig config, 
        IEqualityComparer<T> comparer)
    {
        config.CollectionItemComparers[typeof(T)] = new EqualityComparerAdapter<T>(comparer);
        return config;
    }

    public static ComparisonConfig WithNullHandling(
        this ComparisonConfig config, 
        NullHandling handling)
    {
        config.NullValueHandling = handling;
        return config;
    }

    public static ComparisonConfig EnableDeepComparison(
        this ComparisonConfig config, 
        bool enable = true)
    {
        config.DeepComparison = enable;
        return config;
    }

    public static ComparisonConfig EnableMetrics(
        this ComparisonConfig config, 
        bool enable = true)
    {
        if (enable)
        {
            config.Logger ??= NullLogger.Instance;
        }
        return config;
    }

    public static ComparisonConfig WithLogger(
        this ComparisonConfig config, 
        ILogger logger)
    {
        config.Logger = logger;
        return config;
    }

    public static ComparisonConfig Clone(this ComparisonConfig config)
    {
        return new ComparisonConfig
        {
            MaxDepth = config.MaxDepth,
            MaxObjectCount = config.MaxObjectCount,
            ComparisonTimeout = config.ComparisonTimeout,
            DeepComparison = config.DeepComparison,
            ExcludedProperties = [..config.ExcludedProperties],
            CustomComparers = new Dictionary<Type, ICustomComparer>(
                config.CustomComparers),
            CollectionItemComparers = new Dictionary<Type, IEqualityComparer>(
                config.CollectionItemComparers),
            NullValueHandling = config.NullValueHandling,
            Logger = config.Logger
        };
    }

    public static ComparisonConfig Combine(
        this ComparisonConfig config1, 
        ComparisonConfig config2)
    {
        var combined = config1.Clone();

        foreach (var property in config2.ExcludedProperties)
        {
            combined.ExcludedProperties.Add(property);
        }

        foreach (var comparer in config2.CustomComparers)
        {
            combined.CustomComparers[comparer.Key] = comparer.Value;
        }

        foreach (var comparer in config2.CollectionItemComparers)
        {
            combined.CollectionItemComparers[comparer.Key] = comparer.Value;
        }

        combined.MaxDepth = Math.Min(config1.MaxDepth, config2.MaxDepth);
        combined.MaxObjectCount = Math.Min(
            config1.MaxObjectCount, 
            config2.MaxObjectCount);
        combined.ComparisonTimeout = config1.ComparisonTimeout < config2.ComparisonTimeout
            ? config1.ComparisonTimeout
            : config2.ComparisonTimeout;

        return combined;
    }
}