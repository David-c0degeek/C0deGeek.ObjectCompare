using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Comparison.Strategies;

/// <summary>
/// Defines the contract for comparison strategies
/// </summary>
public interface IComparisonStrategy
{
    /// <summary>
    /// Determines if this strategy can handle the given type
    /// </summary>
    bool CanHandle(Type type);

    /// <summary>
    /// Compares two objects using this strategy
    /// </summary>
    bool Compare(object? obj1, object? obj2, string path, ComparisonResult result, ComparisonContext context);

    /// <summary>
    /// Gets the priority of this strategy (higher numbers = higher priority)
    /// </summary>
    int Priority { get; }
}