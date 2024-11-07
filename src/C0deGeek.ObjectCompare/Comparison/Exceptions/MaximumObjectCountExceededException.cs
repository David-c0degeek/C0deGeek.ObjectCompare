namespace C0deGeek.ObjectCompare.Comparison.Exceptions;

/// <summary>
/// Exception thrown when the maximum object count is exceeded.
/// </summary>
public class MaximumObjectCountExceededException(int maxObjectCount)
    : ComparisonException($"Maximum object count of {maxObjectCount} exceeded during comparison")
{
    /// <summary>
    /// Gets the maximum object count that was configured.
    /// </summary>
    public int MaxObjectCount { get; } = maxObjectCount;
}