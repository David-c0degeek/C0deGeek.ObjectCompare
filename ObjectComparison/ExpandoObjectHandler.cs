using System.Dynamic;

namespace ObjectComparison;

internal class ExpandoObjectHandler : IDynamicTypeHandler
{
    public bool Compare(object obj1, object obj2, string path, ComparisonResult result, ComparisonConfig config)
    {
        if (obj1 is not IDictionary<string, object> dict1 || obj2 is not IDictionary<string, object> dict2)
            return false;

        var allKeys = dict1.Keys.Union(dict2.Keys).Distinct();
        var isEqual = true;

        foreach (var key in allKeys)
        {
            var hasValue1 = dict1.TryGetValue(key, out var value1);
            var hasValue2 = dict2.TryGetValue(key, out var value2);

            if (!hasValue1 || !hasValue2)
            {
                result.Differences.Add($"Property '{key}' exists in only one object at {path}");
                isEqual = false;
                continue;
            }

            if (value1 == null && value2 == null)
                continue;

            if ((value1 == null) != (value2 == null))
            {
                result.Differences.Add($"Property '{key}' null mismatch at {path}");
                isEqual = false;
                continue;
            }

            // Handle nested dynamic objects
            if (value1 is ExpandoObject)
            {
                var nestedResult = new ComparisonResult();
                if (Compare(value1, value2, $"{path}.{key}", nestedResult, config)) continue;
                result.Differences.AddRange(nestedResult.Differences);
                isEqual = false;
            }
            else
            {
                // Use the standard comparison logic for non-dynamic values
                if (AreValuesEqual(value1, value2, config)) continue;
                result.Differences.Add($"Property '{key}' value mismatch at {path}");
                isEqual = false;
            }
        }

        return isEqual;
    }

    private bool AreValuesEqual(object value1, object value2, ComparisonConfig config)
    {
        // Implement value comparison logic or delegate to main comparer
        // This is a simplified version
        return Equals(value1, value2);
    }
}