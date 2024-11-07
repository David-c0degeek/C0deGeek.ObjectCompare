using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Extensions;

/// <summary>
/// Provides extension methods for comparison operations
/// </summary>
public static class ComparisonExtensions
{
    public static bool DeepEquals<T>(this T? obj1, T? obj2, 
        ComparisonConfig? config = null)
    {
        var comparer = new ObjectComparer(config);
        return comparer.Compare(obj1, obj2).AreEqual;
    }

    public static async Task<bool> DeepEqualsAsync<T>(this T? obj1, T? obj2, 
        ComparisonConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        var comparer = new AsyncObjectComparer(config);
        var result = await comparer.CompareAsync(obj1, obj2, cancellationToken);
        return result.AreEqual;
    }

    public static T? TakeSnapshot<T>(this T? obj, ComparisonConfig? config = null)
    {
        var comparer = new ObjectComparer(config);
        return comparer.TakeSnapshot(obj);
    }

    public static IEnumerable<string> GetDifferences<T>(this T? obj1, T? obj2, 
        ComparisonConfig? config = null)
    {
        var comparer = new ObjectComparer(config);
        var result = comparer.Compare(obj1, obj2);
        return result.Differences;
    }

    public static ComparisonResult CompareWith<T>(this T? obj1, T? obj2, 
        Action<ComparisonConfig>? configure = null)
    {
        var config = new ComparisonConfig();
        configure?.Invoke(config);
        var comparer = new ObjectComparer(config);
        return comparer.Compare(obj1, obj2);
    }

    public static async Task<ComparisonResult> CompareWithAsync<T>(
        this T? obj1, T? obj2,
        Action<ComparisonConfig>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var config = new ComparisonConfig();
        configure?.Invoke(config);
        var comparer = new AsyncObjectComparer(config);
        return await comparer.CompareAsync(obj1, obj2, cancellationToken);
    }

    public static IEnumerable<(string Path, object? Value1, object? Value2)> 
        GetDetailedDifferences<T>(this T? obj1, T? obj2, 
            ComparisonConfig? config = null)
    {
        var result = new ObjectComparer(config).Compare(obj1, obj2);
        return result.DifferentPaths.Select(path =>
        {
            var value1 = GetValueAtPath(obj1, path);
            var value2 = GetValueAtPath(obj2, path);
            return (path, value1, value2);
        });
    }

    public static bool HasSameStructure<T>(this T? obj1, T? obj2)
    {
        var config = new ComparisonConfig
        {
            ComparePrivateFields = false,
            DeepComparison = false,
            CompareReadOnlyProperties = false
        };
        return new ObjectComparer(config).Compare(obj1, obj2).AreEqual;
    }

    private static object? GetValueAtPath(object? obj, string path)
    {
        if (obj == null || string.IsNullOrEmpty(path))
            return null;

        var current = obj;
        var parts = path.Split('.');

        foreach (var part in parts)
        {
            if (current == null)
                return null;

            if (TryGetArrayIndex(part, out var index))
            {
                current = GetArrayValue(current, index);
            }
            else
            {
                var property = current.GetType().GetProperty(part);
                current = property?.GetValue(current);
            }
        }

        return current;
    }

    private static bool TryGetArrayIndex(string part, out int index)
    {
        if (part.StartsWith("[") && part.EndsWith("]"))
        {
            return int.TryParse(part.Trim('[', ']'), out index);
        }
        index = -1;
        return false;
    }

    private static object? GetArrayValue(object obj, int index)
    {
        return obj switch
        {
            Array array => index < array.Length ? array.GetValue(index) : null,
            IList list => index < list.Count ? list[index] : null,
            IEnumerable enumerable => enumerable.Cast<object>().ElementAtOrDefault(index),
            _ => null
        };
    }
}