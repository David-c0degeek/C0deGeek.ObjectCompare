using System.Dynamic;

namespace ObjectComparison;

internal class DynamicObjectHandler : IDynamicTypeHandler
{
    public bool Compare(object obj1, object obj2, string path, ComparisonResult result, ComparisonConfig config)
    {
        var dynamicObj1 = obj1 as DynamicObject;
        var dynamicObj2 = obj2 as DynamicObject;

        if (dynamicObj1 == null || dynamicObj2 == null)
            return false;

        var memberNames = GetMemberNames(dynamicObj1).Union(GetMemberNames(dynamicObj2)).Distinct();
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

    private IEnumerable<string> GetMemberNames(DynamicObject obj)
    {
        var memberNames = new List<string>();
        obj.GetDynamicMemberNames()?.ToList().ForEach(name => memberNames.Add(name));
        return memberNames;
    }

    private object GetMemberValue(DynamicObject obj, string memberName)
    {
        var binder = new CustomGetMemberBinder(memberName);
        obj.TryGetMember(binder, out var result);
        return result;
    }

    private bool AreValuesEqual(object value1, object value2, string path,
        ComparisonResult result, ComparisonConfig config)
    {
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
        if (!Equals(value1, value2))
        {
            result.Differences.Add($"Value mismatch at {path}: {value1} != {value2}");
            return false;
        }

        return true;
    }
}