namespace C0deGeek.ObjectCompare.Enums;

/// <summary>
/// Defines the mode of comparison operation
/// </summary>
public enum ComparisonMode
{
    /// <summary>
    /// Default comparison mode
    /// </summary>
    Default = 0,

    /// <summary>
    /// Strict comparison with exact matching
    /// </summary>
    Strict = 1,

    /// <summary>
    /// Loose comparison with type conversion
    /// </summary>
    Loose = 2,

    /// <summary>
    /// Structure-only comparison
    /// </summary>
    Structure = 3,

    /// <summary>
    /// Value-only comparison
    /// </summary>
    Value = 4,

    /// <summary>
    /// Reference-only comparison
    /// </summary>
    Reference = 5,

    /// <summary>
    /// Semantic comparison
    /// </summary>
    Semantic = 6,

    /// <summary>
    /// Custom comparison mode
    /// </summary>
    Custom = 7
}