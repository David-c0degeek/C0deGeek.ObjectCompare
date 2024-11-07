namespace C0deGeek.ObjectCompare.Common;

/// <summary>
/// Provides guard clauses for parameter validation
/// </summary>
public static class Guard
{
    /// <summary>
    /// Throws ArgumentNullException if the value is null
    /// </summary>
    public static T ThrowIfNull<T>(T? value, string paramName) where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }

    /// <summary>
    /// Throws ArgumentException if the string is null or empty
    /// </summary>
    public static string ThrowIfNullOrEmpty(string? value, string paramName)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, paramName);
        return value;
    }

    /// <summary>
    /// Throws ArgumentOutOfRangeException if value is greater than maximum
    /// </summary>
    public static T ThrowIfGreaterThan<T>(T value, T maximum, string paramName) 
        where T : IComparable<T>
    {
        if (value.CompareTo(maximum) > 0)
        {
            throw new ArgumentOutOfRangeException(paramName, 
                $"Value must not be greater than {maximum}");
        }
        return value;
    }

    /// <summary>
    /// Throws ArgumentOutOfRangeException if value is less than minimum
    /// </summary>
    public static T ThrowIfLessThan<T>(T value, T minimum, string paramName) 
        where T : IComparable<T>
    {
        if (value.CompareTo(minimum) < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, 
                $"Value must not be less than {minimum}");
        }
        return value;
    }

    /// <summary>
    /// Throws ArgumentOutOfRangeException if value is not between minimum and maximum
    /// </summary>
    public static T ThrowIfOutOfRange<T>(T value, T minimum, T maximum, string paramName) 
        where T : IComparable<T>
    {
        if (value.CompareTo(minimum) < 0 || value.CompareTo(maximum) > 0)
        {
            throw new ArgumentOutOfRangeException(paramName, 
                $"Value must be between {minimum} and {maximum}");
        }
        return value;
    }

    /// <summary>
    /// Throws ArgumentException if value is default for type T
    /// </summary>
    public static T ThrowIfDefault<T>(T value, string paramName) where T : struct
    {
        if (EqualityComparer<T>.Default.Equals(value, default))
        {
            throw new ArgumentException("Value cannot be default", paramName);
        }
        return value;
    }

    /// <summary>
    /// Throws ArgumentException if the collection is null or empty
    /// </summary>
    public static IEnumerable<T> ThrowIfNullOrEmpty<T>(IEnumerable<T>? collection, string paramName)
    {
        ArgumentNullException.ThrowIfNull(collection, paramName);
        
        if (!collection.Any())
        {
            throw new ArgumentException("Collection cannot be empty", paramName);
        }
        
        return collection;
    }

    /// <summary>
    /// Throws ObjectDisposedException if the object is disposed
    /// </summary>
    public static void ThrowIfDisposed(bool isDisposed, string objectName)
    {
        if (isDisposed)
        {
            throw new ObjectDisposedException(objectName);
        }
    }

    /// <summary>
    /// Throws InvalidOperationException if the condition is false
    /// </summary>
    public static void ThrowIfInvalidOperation(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}