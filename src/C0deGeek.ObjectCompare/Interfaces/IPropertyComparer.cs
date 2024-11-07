using System.Reflection;
using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for property-level comparison
/// </summary>
public interface IPropertyComparer
{
    bool CompareProperty(object obj1, object obj2, PropertyInfo property, 
        string path, ComparisonResult result);
        
    bool IgnoreProperty(PropertyInfo property);
}