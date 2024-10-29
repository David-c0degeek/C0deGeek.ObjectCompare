namespace ObjectComparison;

internal interface IDynamicTypeHandler
{
    bool Compare(object obj1, object obj2, string path, ComparisonResult result, ComparisonConfig config);
}