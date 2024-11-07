using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

public interface IComparisonStrategy
{
    bool Compare(object? obj1, object? obj2, string path, ComparisonResult result, ComparisonContext context);
}