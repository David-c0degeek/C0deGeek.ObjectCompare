using System.Reflection;
using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for complex property comparison
/// </summary>
public interface IComplexPropertyComparer : IPropertyComparer
{
    bool CompareComplexProperty(object obj1, object obj2, 
        PropertyInfo property, string path, 
        ComparisonResult result, ComparisonContext context);
}