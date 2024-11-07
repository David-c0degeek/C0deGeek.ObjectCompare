using System.Runtime.CompilerServices;

namespace C0deGeek.ObjectCompare.Comparison.Exceptions;

/// <summary>
/// Exception thrown when a circular reference is detected during comparison
/// </summary>
public class CircularReferenceException : ComparisonException
{
    /// <summary>
    /// Gets the type of object where the circular reference was detected
    /// </summary>
    public new Type ObjectType { get; }

    /// <summary>
    /// Gets the property path that led to the circular reference
    /// </summary>
    public IReadOnlyList<string> PropertyPath { get; }

    public CircularReferenceException(string path, Type objectType)
        : base($"Circular reference detected for type {objectType.Name}", path, objectType)
    {
        ObjectType = objectType;
        PropertyPath = Array.Empty<string>();
        AddContext("ObjectType", objectType.Name);
    }

    public CircularReferenceException(string path, Type objectType, IEnumerable<string> propertyPath)
        : base($"Circular reference detected for type {objectType.Name}", path, objectType)
    {
        ObjectType = objectType;
        PropertyPath = propertyPath.ToList();
        AddContext("ObjectType", objectType.Name);
        AddContext("PropertyPath", string.Join(" -> ", PropertyPath));
    }

    public CircularReferenceException(string path, Type objectType, Exception inner)
        : base($"Circular reference detected for type {objectType.Name}", path, objectType, inner)
    {
        ObjectType = objectType;
        PropertyPath = Array.Empty<string>();
        AddContext("ObjectType", objectType.Name);
    }

    /// <summary>
    /// Creates an exception instance with detailed object information
    /// </summary>
    public static CircularReferenceException CreateWithDetail(
        string path, Type objectType, IEnumerable<string> propertyPath, object targetObject)
    {
        var exception = new CircularReferenceException(path, objectType, propertyPath);
        exception.AddContext("ObjectHashCode", RuntimeHelpers.GetHashCode(targetObject));
        return exception;
    }
}