namespace C0deGeek.ObjectCompare.Comparison.Exceptions;

/// <summary>
/// Exception thrown when the maximum comparison depth is exceeded
/// </summary>
public class MaximumDepthExceededException : ComparisonException
{
    /// <summary>
    /// Gets the maximum depth that was configured
    /// </summary>
    public int MaxDepth { get; }

    /// <summary>
    /// Gets the type of the object where the maximum depth was reached
    /// </summary>
    public new Type ObjectType { get; }

    public MaximumDepthExceededException(string path, int maxDepth, Type objectType)
        : base($"Maximum comparison depth of {maxDepth} exceeded", path, objectType)
    {
        MaxDepth = maxDepth;
        ObjectType = objectType;
        AddContext("MaxDepth", maxDepth);
        AddContext("ObjectType", objectType.Name);
    }

    public MaximumDepthExceededException(string path, int maxDepth, Type objectType, Exception inner)
        : base($"Maximum comparison depth of {maxDepth} exceeded", path, objectType, inner)
    {
        MaxDepth = maxDepth;
        ObjectType = objectType;
        AddContext("MaxDepth", maxDepth);
        AddContext("ObjectType", objectType.Name);
    }

    /// <summary>
    /// Creates an exception instance with detailed property path information
    /// </summary>
    public static MaximumDepthExceededException CreateWithPropertyPath(
        string path, int maxDepth, Type objectType, IEnumerable<string> propertyPath)
    {
        var exception = new MaximumDepthExceededException(path, maxDepth, objectType);
        exception.AddContext("PropertyPath", string.Join(" -> ", propertyPath));
        return exception;
    }
}