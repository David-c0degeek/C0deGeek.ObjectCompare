namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for handling property access
/// </summary>
public interface IPropertyAccessor
{
    object? GetValue(object obj, string propertyName);
    void SetValue(object obj, string propertyName, object? value);
    bool HasProperty(string propertyName);
}