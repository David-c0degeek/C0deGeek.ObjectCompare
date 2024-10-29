using System.Dynamic;

namespace ObjectComparison;

internal sealed class ExpandoObjectHandler : IDynamicTypeHandler
{
    public bool Compare(object obj1, object obj2, string path, ComparisonResult result, ComparisonConfig config)
    {
        ArgumentNullException.ThrowIfNull(obj1);
        ArgumentNullException.ThrowIfNull(obj2);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(config);

        if (obj1 is not IDictionary<string, object?> dict1 || obj2 is not IDictionary<string, object?> dict2)
        {
            return false;
        }

        var allKeys = dict1.Keys.Union(dict2.Keys).Distinct().ToList();
        var isEqual = true;

        foreach (var key in allKeys)
        {
            if (!CompareExpandoValues(dict1, dict2, key, path, result, config))
            {
                isEqual = false;
            }
        }

        return isEqual;
    }

    private bool CompareExpandoValues(
        IDictionary<string, object?> dict1,
        IDictionary<string, object?> dict2,
        string key,
        string path,
        ComparisonResult result,
        ComparisonConfig config)
    {
        var hasValue1 = dict1.TryGetValue(key, out var value1);
        var hasValue2 = dict2.TryGetValue(key, out var value2);

        if (!hasValue1 || !hasValue2)
        {
            result.Differences.Add($"Property '{key}' exists in only one object at {path}");
            return false;
        }

        if (value1 is null && value2 is null) return true;
        if (value1 is null || value2 is null)
        {
            result.Differences.Add($"Property '{key}' null mismatch at {path}");
            return false;
        }

        // Handle nested dynamic objects
        if (value1 is ExpandoObject)
        {
            return Compare(value1, value2, $"{path}.{key}", result, config);
        }

        // Use the standard comparison logic for non-dynamic values
        if (!AreValuesEqual(value1, value2, config))
        {
            result.Differences.Add($"Property '{key}' value mismatch at {path}");
            return false;
        }

        return true;
    }

    private static bool AreValuesEqual(object value1, object value2, ComparisonConfig config)
    {
        ArgumentNullException.ThrowIfNull(value1);
        ArgumentNullException.ThrowIfNull(value2);
        ArgumentNullException.ThrowIfNull(config);

        // Check if we have a custom comparer for this type
        var type = value1.GetType();
        if (config.CustomComparers.TryGetValue(type, out var customComparer))
        {
            return customComparer.AreEqual(value1, value2, config);
        }

        return value1.Equals(value2);
    }
}