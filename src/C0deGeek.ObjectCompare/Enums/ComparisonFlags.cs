namespace C0deGeek.ObjectCompare.Enums;

/// <summary>
/// Defines bitwise flags for controlling comparison behavior
/// </summary>
[Flags]
public enum ComparisonFlags
{
    /// <summary>
    /// No special comparison behavior
    /// </summary>
    None = 0,

    /// <summary>
    /// Compare private members
    /// </summary>
    IncludePrivateMembers = 1 << 0,

    /// <summary>
    /// Compare read-only properties
    /// </summary>
    IncludeReadOnlyProperties = 1 << 1,

    /// <summary>
    /// Ignore case when comparing strings
    /// </summary>
    IgnoreCase = 1 << 2,

    /// <summary>
    /// Ignore whitespace when comparing strings
    /// </summary>
    IgnoreWhitespace = 1 << 3,

    /// <summary>
    /// Ignore collection order
    /// </summary>
    IgnoreCollectionOrder = 1 << 4,

    /// <summary>
    /// Use deep comparison
    /// </summary>
    DeepComparison = 1 << 5,

    /// <summary>
    /// Track property paths
    /// </summary>
    TrackPaths = 1 << 6,

    /// <summary>
    /// Enable caching
    /// </summary>
    EnableCaching = 1 << 7,

    /// <summary>
    /// Enable parallel processing
    /// </summary>
    EnableParallel = 1 << 8,

    /// <summary>
    /// Continue on finding differences
    /// </summary>
    ContinueOnDifference = 1 << 9,

    /// <summary>
    /// Use type conversion
    /// </summary>
    UseTypeConversion = 1 << 10,

    /// <summary>
    /// Strict null comparison
    /// </summary>
    StrictNullComparison = 1 << 11,

    /// <summary>
    /// Track memory usage
    /// </summary>
    TrackMemoryUsage = 1 << 12,

    /// <summary>
    /// Common flags combination
    /// </summary>
    Default = DeepComparison | EnableCaching | TrackPaths,

    /// <summary>
    /// Performance-oriented flags combination
    /// </summary>
    HighPerformance = EnableCaching | EnableParallel | 
                      IgnoreCollectionOrder | UseTypeConversion,

    /// <summary>
    /// Strict comparison flags combination
    /// </summary>
    Strict = DeepComparison | StrictNullComparison | 
             IncludePrivateMembers | IncludeReadOnlyProperties,

    /// <summary>
    /// Debug-friendly flags combination
    /// </summary>
    Debug = Default | TrackPaths | TrackMemoryUsage | 
            ContinueOnDifference | IncludePrivateMembers
}