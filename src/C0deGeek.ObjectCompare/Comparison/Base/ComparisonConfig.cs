using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Enums;
using C0deGeek.ObjectCompare.Interfaces;
using Microsoft.Extensions.Logging;

namespace C0deGeek.ObjectCompare.Comparison.Base;

/// <summary>
/// Configuration options for comparison operations
/// </summary>
public class ComparisonConfig
{
    /// <summary>
    /// Whether to continue when differences are found
    /// </summary>
    public bool ContinueOnDifference { get; set; }
    
    /// <summary>
    /// Whether to compare private fields and properties
    /// </summary>
    public bool ComparePrivateFields { get; set; }

    /// <summary>
    /// Whether to perform deep comparison of objects
    /// </summary>
    public bool DeepComparison { get; set; } = true;

    /// <summary>
    /// Number of decimal places to consider when comparing decimal values
    /// </summary>
    public int DecimalPrecision { get; set; } = 4;

    /// <summary>
    /// Properties to exclude from comparison
    /// </summary>
    public HashSet<string> ExcludedProperties { get; set; } = [];

    /// <summary>
    /// Custom comparers for specific types
    /// </summary>
    public Dictionary<Type, ICustomComparer> CustomComparers { get; set; } = new();

    /// <summary>
    /// Custom equality comparers for collection items of specific types
    /// </summary>
    public Dictionary<Type, IEqualityComparer> CollectionItemComparers { get; set; } 
        = new();

    /// <summary>
    /// Whether to ignore the order of items in collections
    /// </summary>
    public bool IgnoreCollectionOrder { get; set; }

    /// <summary>
    /// How to handle null values in reference types
    /// </summary>
    public NullHandling NullValueHandling { get; set; } = NullHandling.Strict;

    /// <summary>
    /// Maximum depth for comparison to prevent stack overflow
    /// </summary>
    public int MaxDepth { get; set; } = 100;

    /// <summary>
    /// Maximum number of objects to compare
    /// </summary>
    public int MaxObjectCount { get; set; } = 10000;

    /// <summary>
    /// Whether to use cached reflection metadata
    /// </summary>
    public bool UseCachedMetadata { get; set; } = true;

    /// <summary>
    /// Optional logger for diagnostics
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Whether to track property access paths for better error reporting
    /// </summary>
    public bool TrackPropertyPaths { get; set; } = true;

    /// <summary>
    /// Whether to compare read-only properties
    /// </summary>
    public bool CompareReadOnlyProperties { get; set; } = true;

    /// <summary>
    /// Relative tolerance for floating-point comparisons
    /// </summary>
    public double FloatingPointTolerance { get; set; } = 1e-10;

    /// <summary>
    /// Whether to use relative tolerance for floating-point comparisons
    /// </summary>
    public bool UseRelativeFloatingPointComparison { get; set; } = true;

    /// <summary>
    /// Timeout for comparison operations
    /// </summary>
    public TimeSpan ComparisonTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Creates a clone of the configuration
    /// </summary>
    public ComparisonConfig Clone()
    {
        return new ComparisonConfig
        {
            ComparePrivateFields = ComparePrivateFields,
            DeepComparison = DeepComparison,
            DecimalPrecision = DecimalPrecision,
            ExcludedProperties = [..ExcludedProperties],
            CustomComparers = new Dictionary<Type, ICustomComparer>(CustomComparers),
            CollectionItemComparers = new Dictionary<Type, IEqualityComparer>(CollectionItemComparers),
            IgnoreCollectionOrder = IgnoreCollectionOrder,
            NullValueHandling  = NullValueHandling,
            MaxDepth = MaxDepth,
            MaxObjectCount = MaxObjectCount,
            UseCachedMetadata = UseCachedMetadata,
            Logger = Logger,
            TrackPropertyPaths = TrackPropertyPaths,
            CompareReadOnlyProperties = CompareReadOnlyProperties,
            FloatingPointTolerance = FloatingPointTolerance,
            UseRelativeFloatingPointComparison = UseRelativeFloatingPointComparison,
            ComparisonTimeout = ComparisonTimeout
        };
    }
    
    public void AddComparer<T>(IEqualityComparer<T> comparer)
    {
        CollectionItemComparers[typeof(T)] = new EqualityComparerAdapter<T>(comparer);
    }

    public class Builder
    {
        private readonly ComparisonConfig _config = new();

        public Builder WithPrivateFields(bool compare = true)
        {
            _config.ComparePrivateFields = compare;
            return this;
        }

        public Builder WithDeepComparison(bool deep = true)
        {
            _config.DeepComparison = deep;
            return this;
        }

        public Builder WithDecimalPrecision(int precision)
        {
            _config.DecimalPrecision = precision;
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

        public Builder WithCollectionItemComparer<T>(IEqualityComparer<T> comparer)
        {
            _config.CollectionItemComparers[typeof(T)] = new EqualityComparerAdapter<T>(comparer);
            return this;
        }

        public Builder IgnoreCollectionOrder(bool ignore = true)
        {
            _config.IgnoreCollectionOrder = ignore;
            return this;
        }

        public Builder WithNullHandling(NullHandling handling)
        {
            _config.NullValueHandling = handling;
            return this;
        }

        public Builder WithMaxDepth(int maxDepth)
        {
            Guard.ThrowIfLessThan(maxDepth, 1, nameof(maxDepth));
            _config.MaxDepth = maxDepth;
            return this;
        }

        public Builder WithMaxObjectCount(int maxCount)
        {
            Guard.ThrowIfLessThan(maxCount, 1, nameof(maxCount));
            _config.MaxObjectCount = maxCount;
            return this;
        }

        public Builder UseCachedMetadata(bool useCache = true)
        {
            _config.UseCachedMetadata = useCache;
            return this;
        }

        public Builder WithLogger(ILogger logger)
        {
            _config.Logger = Guard.ThrowIfNull(logger, nameof(logger));
            return this;
        }

        public Builder TrackPropertyPaths(bool track = true)
        {
            _config.TrackPropertyPaths = track;
            return this;
        }

        public Builder CompareReadOnlyProperties(bool compare = true)
        {
            _config.CompareReadOnlyProperties = compare;
            return this;
        }

        public Builder WithFloatingPointTolerance(double tolerance)
        {
            Guard.ThrowIfLessThan(tolerance, 0, nameof(tolerance));
            _config.FloatingPointTolerance = tolerance;
            return this;
        }

        public Builder UseRelativeFloatingPointComparison(bool useRelative = true)
        {
            _config.UseRelativeFloatingPointComparison = useRelative;
            return this;
        }

        public Builder WithComparisonTimeout(TimeSpan timeout)
        {
            _config.ComparisonTimeout = Guard.ThrowIfLessThan(
                timeout, TimeSpan.Zero, nameof(timeout));
            return this;
        }

        public ComparisonConfig Build()
        {
            return _config.Clone();
        }
    }
}