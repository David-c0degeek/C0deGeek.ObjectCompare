namespace C0deGeek.ObjectCompare.Common;

/// <summary>
/// Provides standardized exception messages and creation methods
/// </summary>
internal static class ExceptionHelper
{
    public static string CreatePropertyAccessMessage(string propertyName, Type objectType, string path)
    {
        return $"Failed to access property '{propertyName}' on type '{objectType.Name}' at path: {path}";
    }

    public static string CreateCollectionComparisonMessage(Type collectionType, string path)
    {
        return $"Failed to compare collection of type '{collectionType.Name}' at path: {path}";
    }

    public static string CreateTypeComparisonMessage(Type type1, Type type2, string path)
    {
        return $"Cannot compare objects of different types at path: {path}. " +
               $"Type1: {type1.Name}, Type2: {type2.Name}";
    }

    public static string CreateCloneFailureMessage(Type objectType)
    {
        return $"Failed to create clone of type '{objectType.Name}'. " +
               "Ensure the type has a parameterless constructor or is properly configured for cloning.";
    }

    public static string CreateMaxDepthMessage(int maxDepth, string path)
    {
        return $"Maximum comparison depth of {maxDepth} reached at path: {path}";
    }

    public static string CreateMaxObjectCountMessage(int maxCount)
    {
        return $"Maximum object count of {maxCount} exceeded during comparison";
    }

    public static string CreateCircularReferenceMessage(Type objectType, string path)
    {
        return $"Circular reference detected for type '{objectType.Name}' at path: {path}";
    }

    public static string CreateInvalidConfigurationMessage(string setting, string reason)
    {
        return $"Invalid configuration setting '{setting}': {reason}";
    }

    public static string CreateResourceExhaustionMessage(string resource)
    {
        return $"Resource exhaustion occurred while accessing: {resource}";
    }

    public static string CreateConcurrencyMessage(string operation)
    {
        return $"Concurrency violation occurred during: {operation}";
    }

    public static string CreateTimeoutMessage(string operation, TimeSpan timeout)
    {
        return $"Operation '{operation}' timed out after {timeout.TotalSeconds} seconds";
    }

    public static string CreateValidationMessage(string field, string reason)
    {
        return $"Validation failed for '{field}': {reason}";
    }
}