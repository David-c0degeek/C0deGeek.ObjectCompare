using C0deGeek.ObjectCompare.Enums;
using C0deGeek.ObjectCompare.Interfaces;
using Microsoft.Extensions.Logging;

namespace C0deGeek.ObjectCompare.Configuration;

/// <summary>
/// Provides comprehensive configuration options for comparison operations
/// </summary>
public class ComparisonConfiguration
{
    public ComparisonMode Mode { get; set; } = ComparisonMode.Default;
    public bool DeepComparison { get; set; } = true;
    public int MaxDepth { get; set; } = 100;
    public int MaxObjectCount { get; set; } = 10000;
    public TimeSpan ComparisonTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool ContinueOnDifference { get; set; }
    public HashSet<string> ExcludedProperties { get; set; } = [];
    public Dictionary<Type, ICustomComparer> CustomComparers { get; set; } = new();
    public Dictionary<Type, IEqualityComparer> CollectionItemComparers { get; set; } = new();
    public NullHandling NullHandling { get; set; } = NullHandling.Strict;

    public ComparisonOptions Options { get; set; } = new();
    public PerformanceOptions Performance { get; set; } = new();
    public LoggingOptions Logging { get; set; } = new();
    public CachingOptions Caching { get; set; } = new();

    public class ComparisonOptions
    {
        public bool ComparePrivateFields { get; set; }
        public bool CompareReadOnlyProperties { get; set; } = true;
        public bool IgnoreCase { get; set; }
        public bool IgnoreCollectionOrder { get; set; }
        public bool IgnoreWhitespace { get; set; }
        public double FloatingPointTolerance { get; set; } = 1e-10;
        public int DecimalPrecision { get; set; } = 4;
        public bool UseRelativeFloatingPointComparison { get; set; } = true;
    }

    public class PerformanceOptions
    {
        public bool EnableParallelProcessing { get; set; }
        public int MaxDegreeOfParallelism { get; set; } = -1;
        public bool EnableMetrics { get; set; }
        public TimeSpan MetricsAggregationInterval { get; set; } = TimeSpan.FromMinutes(1);
        public bool TrackMemoryUsage { get; set; }
        public int BatchSize { get; set; } = 100;
    }

    public class LoggingOptions
    {
        public bool EnableDetailedLogging { get; set; }
        public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
        public bool IncludeTimings { get; set; }
        public bool TrackPropertyPaths { get; set; } = true;
        public ILogger? Logger { get; set; }
    }

    public class CachingOptions
    {
        public bool EnableCaching { get; set; } = true;
        public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromHours(1);
        public int MaxCacheSize { get; set; } = 10000;
        public bool EnableMetadataCaching { get; set; } = true;
        public bool EnableExpressionCaching { get; set; } = true;
    }

    public ComparisonConfiguration Clone()
    {
        return new ComparisonConfiguration
        {
            Mode = Mode,
            DeepComparison = DeepComparison,
            MaxDepth = MaxDepth,
            MaxObjectCount = MaxObjectCount,
            ComparisonTimeout = ComparisonTimeout,
            ContinueOnDifference = ContinueOnDifference,
            ExcludedProperties = [..ExcludedProperties],
            CustomComparers = new Dictionary<Type, ICustomComparer>(CustomComparers),
            CollectionItemComparers = new Dictionary<Type, IEqualityComparer>(
                CollectionItemComparers),
            NullHandling = NullHandling,
            Options = new ComparisonOptions
            {
                ComparePrivateFields = Options.ComparePrivateFields,
                CompareReadOnlyProperties = Options.CompareReadOnlyProperties,
                IgnoreCase = Options.IgnoreCase,
                IgnoreCollectionOrder = Options.IgnoreCollectionOrder,
                IgnoreWhitespace = Options.IgnoreWhitespace,
                FloatingPointTolerance = Options.FloatingPointTolerance,
                DecimalPrecision = Options.DecimalPrecision,
                UseRelativeFloatingPointComparison = Options.UseRelativeFloatingPointComparison
            },
            Performance = new PerformanceOptions
            {
                EnableParallelProcessing = Performance.EnableParallelProcessing,
                MaxDegreeOfParallelism = Performance.MaxDegreeOfParallelism,
                EnableMetrics = Performance.EnableMetrics,
                MetricsAggregationInterval = Performance.MetricsAggregationInterval,
                TrackMemoryUsage = Performance.TrackMemoryUsage,
                BatchSize = Performance.BatchSize
            },
            Logging = new LoggingOptions
            {
                EnableDetailedLogging = Logging.EnableDetailedLogging,
                MinimumLevel = Logging.MinimumLevel,
                IncludeTimings = Logging.IncludeTimings,
                TrackPropertyPaths = Logging.TrackPropertyPaths,
                Logger = Logging.Logger
            },
            Caching = new CachingOptions
            {
                EnableCaching = Caching.EnableCaching,
                CacheTimeout = Caching.CacheTimeout,
                MaxCacheSize = Caching.MaxCacheSize,
                EnableMetadataCaching = Caching.EnableMetadataCaching,
                EnableExpressionCaching = Caching.EnableExpressionCaching
            }
        };
    }

    public class Builder
    {
        private readonly ComparisonConfiguration _config = new();

        public Builder WithMode(ComparisonMode mode)
        {
            _config.Mode = mode;
            return this;
        }

        public Builder WithMaxDepth(int maxDepth)
        {
            _config.MaxDepth = maxDepth;
            return this;
        }

        public Builder WithTimeout(TimeSpan timeout)
        {
            _config.ComparisonTimeout = timeout;
            return this;
        }

        public Builder ExcludeProperty(string propertyName)
        {
            _config.ExcludedProperties.Add(propertyName);
            return this;
        }

        public Builder WithCustomComparer<T>(ICustomComparer comparer)
        {
            _config.CustomComparers[typeof(T)] = comparer;
            return this;
        }

        public Builder WithNullHandling(NullHandling handling)
        {
            _config.NullHandling = handling;
            return this;
        }

        public Builder WithComparisonOptions(Action<ComparisonOptions> configure)
        {
            configure(_config.Options);
            return this;
        }

        public Builder WithPerformanceOptions(Action<PerformanceOptions> configure)
        {
            configure(_config.Performance);
            return this;
        }

        public Builder WithLoggingOptions(Action<LoggingOptions> configure)
        {
            configure(_config.Logging);
            return this;
        }

        public Builder WithCachingOptions(Action<CachingOptions> configure)
        {
            configure(_config.Caching);
            return this;
        }

        public ComparisonConfiguration Build()
        {
            return _config.Clone();
        }
    }
}