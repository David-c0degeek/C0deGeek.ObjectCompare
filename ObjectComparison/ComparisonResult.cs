namespace ObjectComparison;

/// <summary>
/// Detailed results of object comparison
/// </summary>
public class ComparisonResult
{
    /// <summary>
    /// Whether the objects are considered equal
    /// </summary>
    public bool AreEqual { get; set; } = true;

    /// <summary>
    /// List of differences found during comparison
    /// </summary>
    public List<string> Differences { get; set; } = new();

    /// <summary>
    /// The path where comparison stopped (if max depth was reached)
    /// </summary>
    public string MaxDepthPath { get; set; }

    /// <summary>
    /// Time taken to perform the comparison
    /// </summary>
    public TimeSpan ComparisonTime { get; set; }

    /// <summary>
    /// Number of objects compared
    /// </summary>
    public int ObjectsCompared { get; set; }

    /// <summary>
    /// Number of properties compared
    /// </summary>
    public int PropertiesCompared { get; set; }

    /// <summary>
    /// Maximum depth reached during comparison
    /// </summary>
    public int MaxDepthReached { get; set; }

    /// <summary>
    /// Collection of property paths that were different
    /// </summary>
    public HashSet<string> DifferentPaths { get; } = new();
}