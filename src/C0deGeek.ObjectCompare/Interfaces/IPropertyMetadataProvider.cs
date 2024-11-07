using System.Reflection;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for property metadata handling
/// </summary>
public interface IPropertyMetadataProvider
{
    Type? GetPropertyType(string propertyName);
    bool IsReadOnly(string propertyName);
    bool IsComplex(string propertyName);
    IEnumerable<PropertyInfo> GetProperties(bool includeNonPublic = false);
}