using C0deGeek.ObjectCompare.Metadata;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for metadata caching
/// </summary>
public interface IMetadataCache
{
    TypeMetadata GetOrAddMetadata(Type type);
    void InvalidateCache(Type type);
    void ClearCache();
}