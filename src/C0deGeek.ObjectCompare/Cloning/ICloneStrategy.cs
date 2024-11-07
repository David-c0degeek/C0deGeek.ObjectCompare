namespace C0deGeek.ObjectCompare.Cloning;

/// <summary>
/// Defines the contract for object cloning strategies
/// </summary>
public interface ICloneStrategy
{
    /// <summary>
    /// Determines if this strategy can handle the given type
    /// </summary>
    bool CanHandle(Type type);

    /// <summary>
    /// Creates a deep clone of the specified object
    /// </summary>
    object? Clone(object? obj, CloneContext context);

    /// <summary>
    /// Gets the priority of this strategy (higher numbers = higher priority)
    /// </summary>
    int Priority { get; }
}