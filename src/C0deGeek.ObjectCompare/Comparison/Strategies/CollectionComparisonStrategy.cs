using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Comparison.Strategies;

/// <summary>
/// Strategy for comparing collections and arrays
/// </summary>
public class CollectionComparisonStrategy(ComparisonConfig config) : ComparisonStrategyBase(config)
{
    private readonly Dictionary<Type, IComparisonStrategy> _itemStrategies = new()
    {
        { typeof(ValueType), new SimpleTypeComparisonStrategy(config) },
        { typeof(object), new ComplexTypeComparisonStrategy(config) }
    };

    public override bool CanHandle(Type type)
    {
        return typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
    }

    public override int Priority => 50;

    public override bool Compare(object? obj1, object? obj2, string path, 
        ComparisonResult result, ComparisonContext context)
    {
        LogComparison(nameof(CollectionComparisonStrategy), path, obj1, obj2);

        if (HandleNulls(obj1, obj2, path, result)) return result.AreEqual;

        var collection1 = (IEnumerable)obj1!;
        var collection2 = (IEnumerable)obj2!;

        var list1 = collection1.Cast<object>().ToList();
        var list2 = collection2.Cast<object>().ToList();

        if (list1.Count != list2.Count)
        {
            result.AddDifference(
                $"Collection lengths differ: {list1.Count} != {list2.Count}", path);
            result.AreEqual = false;
            return false;
        }

        return Config.IgnoreCollectionOrder
            ? CompareUnordered(list1, list2, path, result, context)
            : CompareOrdered(list1, list2, path, result, context);
    }

    private bool CompareOrdered(List<object> list1, List<object> list2, 
        string path, ComparisonResult result, ComparisonContext context)
    {
        for (var i = 0; i < list1.Count; i++)
        {
            var itemPath = $"{path}[{i}]";
            var item1 = list1[i];
            var item2 = list2[i];

            var itemType = item1?.GetType() ?? item2?.GetType() ?? typeof(object);
            var strategy = GetItemStrategy(itemType);

            if (!strategy.Compare(item1, item2, itemPath, result, context))
            {
                result.AreEqual = false;
                return false;
            }
        }

        return true;
    }

    private bool CompareUnordered(List<object> list1, List<object> list2, 
        string path, ComparisonResult result, ComparisonContext context)
    {
        var matched = new bool[list2.Count];

        for (var i = 0; i < list1.Count; i++)
        {
            var item1 = list1[i];
            var matchFound = false;

            for (var j = 0; j < list2.Count; j++)
            {
                if (matched[j]) continue;

                var tempResult = new ComparisonResult();
                var itemType = item1?.GetType() ?? typeof(object);
                var strategy = GetItemStrategy(itemType);

                if (!strategy.Compare(item1, list2[j], $"{path}[{i}]", tempResult, context))
                    continue;

                matched[j] = true;
                matchFound = true;
                break;
            }

            if (!matchFound)
            {
                result.AddDifference(
                    $"No matching item found for element at index {i}", path);
                result.AreEqual = false;
                return false;
            }
        }

        return true;
    }

    private IComparisonStrategy GetItemStrategy(Type itemType)
    {
        foreach (var kvp in _itemStrategies)
        {
            if (kvp.Key.IsAssignableFrom(itemType))
            {
                return kvp.Value;
            }
        }

        return _itemStrategies[typeof(object)];
    }
}