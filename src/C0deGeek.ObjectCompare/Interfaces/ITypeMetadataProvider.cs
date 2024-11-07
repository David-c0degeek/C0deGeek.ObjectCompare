using C0deGeek.ObjectCompare.Metadata;

namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Interface for type metadata providers
/// </summary>
public interface ITypeMetadataProvider
{
    TypeMetadata GetMetadata(Type type);
    bool IsSimpleType(Type type);
    bool IsCollection(Type type);
    Type? GetCollectionElementType(Type type);
}