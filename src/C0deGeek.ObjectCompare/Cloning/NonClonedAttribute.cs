namespace C0deGeek.ObjectCompare.Cloning;

/// <summary>
/// Attribute to mark properties that should not be cloned
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NonClonedAttribute : Attribute
{
}