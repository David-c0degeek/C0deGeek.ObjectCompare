using System.Collections.Concurrent;
using System.Text;

namespace C0deGeek.ObjectCompare.Comparison.Base;

/// <summary>
/// Contains the detailed results of a comparison operation
/// </summary>
public class ComparisonResult
{
    private readonly List<string> _differences = [];
    private readonly HashSet<string> _differentPaths = [];
    private readonly ConcurrentDictionary<string, object> _metadata = new();

    /// <summary>
    /// Whether the compared objects are considered equal
    /// </summary>
    public bool AreEqual { get; set; } = true;

    /// <summary>
    /// List of differences found during comparison
    /// </summary>
    public List<string> Differences => _differences;

    /// <summary>
    /// The path where comparison stopped (if max depth was reached)
    /// </summary>
    public string? MaxDepthPath { get; set; }

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
    public IReadOnlySet<string> DifferentPaths => _differentPaths;

    public void AddDifference(string difference, string path)
    {
        _differences.Add(difference);
        _differentPaths.Add(path);
    }

    public void AddDifferences(IEnumerable<string> differences)
    {
        _differences.AddRange(differences);
    }

    public void SetMetadata<T>(string key, T value)
    {
        _metadata[key] = value!;
    }

    public T? GetMetadata<T>(string key)
    {
        return _metadata.TryGetValue(key, out var value) ? (T)value : default;
    }

    public ComparisonResult Clone()
    {
        return new ComparisonResult
        {
            AreEqual = AreEqual,
            MaxDepthPath = MaxDepthPath,
            ComparisonTime = ComparisonTime,
            ObjectsCompared = ObjectsCompared,
            PropertiesCompared = PropertiesCompared,
            MaxDepthReached = MaxDepthReached
        };
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Comparison Result: {(AreEqual ? "Equal" : "Not Equal")}");
        builder.AppendLine($"Objects Compared: {ObjectsCompared}");
        builder.AppendLine($"Properties Compared: {PropertiesCompared}");
        builder.AppendLine($"Max Depth Reached: {MaxDepthReached}");
        builder.AppendLine($"Time Taken: {ComparisonTime.TotalMilliseconds}ms");

        if (Differences.Count > 0)
        {
            builder.AppendLine("Differences:");
            foreach (var difference in Differences)
            {
                builder.AppendLine($"- {difference}");
            }
        }

        return builder.ToString();
    }
}