namespace ObjectComparison;

/// <summary>
/// Exception thrown during comparison operations
/// </summary>
public class ComparisonException : Exception
{
    public string Path { get; }

    public ComparisonException(string message) : base(message)
    {
    }

    public ComparisonException(string message, string path) : base(message)
    {
        Path = path;
    }

    public ComparisonException(string message, string path, Exception inner)
        : base(message, inner)
    {
        Path = path;
    }
}