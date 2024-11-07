using System.Text;

namespace C0deGeek.ObjectCompare.Comparison.Exceptions;

/// <summary>
/// Base exception for all comparison-related errors
/// </summary>
public class ComparisonException : Exception
{
    /// <summary>
    /// Gets the path in the object graph where the exception occurred
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// Gets the type of object being compared when the exception occurred
    /// </summary>
    public Type? ObjectType { get; }

    /// <summary>
    /// Gets additional context about the comparison operation
    /// </summary>
    public IDictionary<string, object> Context { get; }

    public ComparisonException(string message) : base(message)
    {
        Context = new Dictionary<string, object>();
    }

    public ComparisonException(string message, string path) : base(message)
    {
        Path = path;
        Context = new Dictionary<string, object>();
    }

    public ComparisonException(string message, string path, Type objectType)
        : base(message)
    {
        Path = path;
        ObjectType = objectType;
        Context = new Dictionary<string, object>();
    }

    public ComparisonException(string message, string path, Exception inner)
        : base(message, inner)
    {
        Path = path;
        Context = new Dictionary<string, object>();
    }

    public ComparisonException(string message, string path, Type objectType, Exception inner)
        : base(message, inner)
    {
        Path = path;
        ObjectType = objectType;
        Context = new Dictionary<string, object>();
    }

    /// <summary>
    /// Adds additional context information to the exception
    /// </summary>
    public void AddContext(string key, object value)
    {
        Context[key] = value;
    }

    public override string Message => BuildMessage();

    private string BuildMessage()
    {
        var builder = new StringBuilder(base.Message);

        if (!string.IsNullOrEmpty(Path))
        {
            builder.AppendLine()
                .Append("Path: ").AppendLine(Path);
        }

        if (ObjectType != null)
        {
            builder.Append("Object Type: ").AppendLine(ObjectType.Name);
        }

        if (Context.Count > 0)
        {
            builder.AppendLine("Additional Context:");
            foreach (var kvp in Context)
            {
                builder.Append("  ")
                    .Append(kvp.Key)
                    .Append(": ")
                    .AppendLine(kvp.Value.ToString());
            }
        }

        return builder.ToString();
    }
}