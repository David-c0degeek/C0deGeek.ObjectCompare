using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Comparison.Strategies;

/// <summary>
/// Strategy for comparing collections and arrays
/// </summary>
public class CollectionComparisonStrategy : ComparisonStrategyBase
{
    private readonly IComparisonStrategy _simpleTypeStrategy;
    private readonly IComparisonStrategy _complexTypeStrategy;

    public CollectionComparisonStrategy(ComparisonConfig config) : base(config)
    {
        _simpleTypeStrategy = new SimpleTypeComparisonStrategy(config);
        _complexTypeStrategy = new ComplexTypeComparisonStrategy(config);
    }

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

        var type = obj1!.GetType();
        var typeKey = $"{type.FullName}:{path}";

        // Prevent infinite recursion
        if (!context.AddProcessedType(typeKey))
        {
            // We've seen this type at this path before in the current comparison chain
            // Compare only the reference equality
            return ReferenceEquals(obj1, obj2);
        }

        try
        {
            var collection1 = (IEnumerable)obj1;
            var collection2 = (IEnumerable)obj2;

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
        finally
        {
            context.RemoveProcessedType(typeKey);
        }
    }

    private bool CompareOrdered(List<object> list1, List<object> list2, 
        string path, ComparisonResult result, ComparisonContext context)
    {
        var isEqual = true;
        for (var i = 0; i < list1.Count; i++)
        {
            var itemPath = $"{path}[{i}]";
            context.PushObject(list1[i]);
            try
            {
                var item1 = list1[i];
                var item2 = list2[i];

                if (!CompareItems(item1, item2, itemPath, result, context))
                {
                    isEqual = false;
                    if (!Config.ContinueOnDifference)
                        break;
                }
            }
            finally
            {
                context.PopObject();
            }
        }
        return isEqual;
    }

    private bool CompareUnordered(List<object> list1, List<object> list2, 
        string path, ComparisonResult result, ComparisonContext context)
    {
        var matched = new bool[list2.Count];
        var isEqual = true;

        for (var i = 0; i < list1.Count; i++)
        {
            var matchFound = false;
            var item1 = list1[i];

            context.PushObject(item1);
            try
            {
                for (var j = 0; j < list2.Count; j++)
                {
                    if (matched[j]) continue;

                    var tempResult = new ComparisonResult();
                    if (!CompareItems(item1, list2[j], $"{path}[{i}]", tempResult, context))
                        continue;

                    matched[j] = true;
                    matchFound = true;
                    break;
                }

                if (!matchFound)
                {
                    result.AddDifference(
                        $"No matching item found for element at index {i}", path);
                    isEqual = false;
                    if (!Config.ContinueOnDifference)
                        break;
                }
            }
            finally
            {
                context.PopObject();
            }
        }

        return isEqual;
    }

    private bool CompareItems(object? item1, object? item2, string path, 
        ComparisonResult result, ComparisonContext context)
    {
        if (item1 == null && item2 == null) return true;
        if (item1 == null || item2 == null) return false;

        if (item1.GetType().IsPrimitive || item1 is string)
        {
            return _simpleTypeStrategy.Compare(item1, item2, path, result, context);
        }

        return _complexTypeStrategy.Compare(item1, item2, path, result, context);
    }
}