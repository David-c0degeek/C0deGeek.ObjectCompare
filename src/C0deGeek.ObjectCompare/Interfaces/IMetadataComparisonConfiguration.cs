namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for metadata comparison configuration
/// </summary>
public interface IMetadataComparisonConfiguration
{
    bool IncludePrivateMembers { get; }
    bool CompareReadOnlyProperties { get; }
    ISet<string> ExcludedMembers { get; }
    IDictionary<Type, ICustomComparer> CustomComparers { get; }
}