using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Dynamic;

internal interface IDynamicTypeHandler
{
    bool Compare(object obj1, object obj2, string path, ComparisonResult result, ComparisonConfig config);
}