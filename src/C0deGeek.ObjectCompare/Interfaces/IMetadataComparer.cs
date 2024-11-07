using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Metadata;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for metadata-based comparison
/// </summary>
public interface IMetadataComparer
{
    bool CompareWithMetadata(object obj1, object obj2, 
        TypeMetadata metadata, string path, 
        ComparisonResult result);
}