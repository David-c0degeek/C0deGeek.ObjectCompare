using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace C0deGeek.ObjectCompare.Collections;

/// <summary>
/// Compares collections ignoring element order
/// </summary>
public class UnorderedCollectionComparer : ICollectionComparer
{
    private readonly ComparisonConfig _config;
    private readonly ILogger _logger;

    public UnorderedCollectionComparer(ComparisonConfig config, ILogger? logger = null)
    {
        _config = Guard.ThrowIfNull(config, nameof(config));
        _logger = logger ?? NullLogger.Instance;
    }

    public bool CompareCollections(IEnumerable collection1, IEnumerable collection2, 
        string path, ComparisonResult result)
    {
        var list1 = collection1.Cast<object>().ToList();
        var list2 = collection2.Cast<object>().ToList();

        if (list1.Count != list2.Count)
        {
            result.AddDifference(
                $"Collection lengths differ: {list1.Count} != {list2.Count}", path);
            return false;
        }

        // For simple types, use hash-based comparison
        if (AreSimpleTypes(list1))
        {
            return CompareSimpleTypes(list1, list2, path, result);
        }

        // For complex types, use full comparison
        return CompareComplexTypes(list1, list2, path, result);
    }

    private bool CompareSimpleTypes(List<object> list1, List<object> list2, 
        string path, ComparisonResult result)
    {
        var counts1 = new Dictionary<object, int>(new FastEqualityComparer());
        var counts2 = new Dictionary<object, int>(new FastEqualityComparer());

        // Count occurrences in first list
        foreach (var item in list1)
        {
            if (!counts1.ContainsKey(item))
            {
                counts1[item] = 0;
            }
            counts1[item]++;
        }

        // Count occurrences in second list
        foreach (var item in list2)
        {
            if (!counts2.ContainsKey(item))
            {
                counts2[item] = 0;
            }
            counts2[item]++;
        }

        // Compare counts
        foreach (var kvp in counts1)
        {
            if (!counts2.TryGetValue(kvp.Key, out var count2) || count2 != kvp.Value)
            {
                result.AddDifference(
                    $"Element count mismatch for value {kvp.Key}", path);
                return false;
            }
        }

        return true;
    }

    private bool CompareComplexTypes(List<object> list1, List<object> list2, 
        string path, ComparisonResult result)
    {
        var matched = new bool[list2.Count];
        var comparer = new ObjectComparer(_config);

        for (var i = 0; i < list1.Count; i++)
        {
            var matchFound = false;
            var item1 = list1[i];

            for (var j = 0; j < list2.Count; j++)
            {
                if (matched[j]) continue;

                var itemResult = comparer.Compare(item1, list2[j]);
                if (!itemResult.AreEqual) continue;

                matched[j] = true;
                matchFound = true;
                break;
            }

            if (!matchFound)
            {
                result.AddDifference(
                    $"No matching element found for item at index {i}", path);
                return false;
            }
        }

        return true;
    }

    private static bool AreSimpleTypes(List<object> list)
    {
        return list.All(item => 
            item == null || 
            item.GetType().IsPrimitive || 
            item is string || 
            item is DateTime || 
            item is decimal);
    }

    private class FastEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;

            // For simple types, use built-in equality
            if (x.GetType().IsPrimitive || x is string || x is DateTime || x is decimal)
            {
                return x.Equals(y);
            }

            // For other types, use reference equality
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return obj?.GetHashCode() ?? 0;
        }
    }
}