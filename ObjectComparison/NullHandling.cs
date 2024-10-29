namespace ObjectComparison;

/// <summary>
/// Enum defining how null values should be handled
/// </summary>
public enum NullHandling
{
    /// <summary>
    /// Treat null values as distinct values
    /// </summary>
    Strict,

    /// <summary>
    /// Treat null and empty values as equivalent
    /// </summary>
    Loose
}