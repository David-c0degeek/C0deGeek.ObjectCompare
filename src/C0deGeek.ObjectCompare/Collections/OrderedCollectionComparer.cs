using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Comparison.Exceptions;
using C0deGeek.ObjectCompare.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace C0deGeek.ObjectCompare.Collections;

/// <summary>
/// Compares collections maintaining order of elements
/// </summary>
public class OrderedCollectionComparer(ComparisonConfig config) : ICollectionComparer
{
    private readonly ComparisonConfig _config = Guard.ThrowIfNull(config, nameof(config));
    private readonly ILogger _logger = config.Logger ?? NullLogger.Instance;

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

        var isEqual = true;
        for (var i = 0; i < list1.Count; i++)
        {
            var itemPath = $"{path}[{i}]";
            if (!CompareItems(list1[i], list2[i], itemPath, result))
            {
                isEqual = false;
                if (!_config.ContinueOnDifference)
                {
                    break;
                }
            }
        }

        return isEqual;
    }

    private bool CompareItems(object? item1, object? item2, string path, ComparisonResult result)
    {
        try
        {
            if (ReferenceEquals(item1, item2)) return true;

            if (item1 == null || item2 == null)
            {
                result.AddDifference("Collection item is null", path);
                return false;
            }

            var type = item1.GetType();
            if (_config.CollectionItemComparers.TryGetValue(type, out var itemComparer))
            {
                if (!itemComparer.Equals(item1, item2))
                {
                    result.AddDifference(
                        $"Collection items differ: {item1} != {item2}", path);
                    return false;
                }
                return true;
            }

            // Use the default comparison strategy for complex types
            var comparer = new ObjectComparer(_config);
            var itemResult = comparer.Compare(item1, item2);

            if (!itemResult.AreEqual)
            {
                foreach (var difference in itemResult.Differences)
                {
                    result.AddDifference(difference, path);
                }
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing collection items at path {Path}", path);
            throw new ComparisonException(
                "Error comparing collection items", path, ex);
        }
    }
}