using System.Dynamic;

namespace ObjectComparison;

internal sealed class DynamicObjectHandler : IDynamicTypeHandler
{
    public bool Compare(object obj1, object obj2, string path, ComparisonResult result, ComparisonConfig config)
    {
        ArgumentNullException.ThrowIfNull(obj1);
        ArgumentNullException.ThrowIfNull(obj2);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(config);

        if (obj1 is not DynamicObject dynamicObj1 || obj2 is not DynamicObject dynamicObj2)
        {
            result.Differences.Add($"Objects are not DynamicObject at {path}");
            return false;
        }

        var memberNames = GetMemberNames(dynamicObj1).Union(GetMemberNames(dynamicObj2)).Distinct().ToList();
        var isEqual = true;

        foreach (var memberName in memberNames)
        {
            var value1 = GetMemberValue(dynamicObj1, memberName);
            var value2 = GetMemberValue(dynamicObj2, memberName);

            if (!AreValuesEqual(value1, value2, $"{path}.{memberName}", result, config))
            {
                isEqual = false;
            }
        }

        return isEqual;
    }

    private static IEnumerable<string> GetMemberNames(DynamicObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return obj.GetDynamicMemberNames();
    }

    private static object? GetMemberValue(DynamicObject obj, string memberName)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(memberName);

        var binder = new CustomGetMemberBinder(memberName);
        return obj.TryGetMember(binder, out var result) ? result : null;
    }

    private bool AreValuesEqual(object? value1, object? value2, string path,
        ComparisonResult result, ComparisonConfig config)
    {
        if (ReferenceEquals(value1, value2)) return true;
        if (value1 is null || value2 is null)
        {
            result.Differences.Add($"Null value mismatch at {path}");
            return false;
        }

        // Handle nested dynamic objects
        if (value1 is DynamicObject || value2 is DynamicObject)
        {
            return Compare(value1, value2, path, result, config);
        }

        // Handle ExpandoObjects
        if (value1 is ExpandoObject || value2 is ExpandoObject)
        {
            var handler = new ExpandoObjectHandler();
            return handler.Compare(value1, value2, path, result, config);
        }

        // Handle regular values
        if (value1.Equals(value2)) return true;
        
        result.Differences.Add($"Value mismatch at {path}: {value1} != {value2}");
        return false;

    }
}