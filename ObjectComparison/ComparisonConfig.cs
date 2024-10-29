using System.Collections;
using Microsoft.Extensions.Logging;

namespace ObjectComparison;

/// <summary>
/// Configuration for object comparison with comprehensive options
/// </summary>
public class ComparisonConfig
{
    /// <summary>
    /// Whether to compare private fields and properties
    /// </summary>
    public bool ComparePrivateFields { get; set; } = false;

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
    public Dictionary<Type, IEqualityComparer> CollectionItemComparers { get; set; } = new();

    /// <summary>
    /// Whether to ignore the order of items in collections
    /// </summary>
    public bool IgnoreCollectionOrder { get; set; } = false;

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
    public ILogger Logger { get; set; }

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
}