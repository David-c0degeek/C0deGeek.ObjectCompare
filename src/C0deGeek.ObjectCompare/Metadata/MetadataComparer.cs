using System.Collections.Concurrent;
using System.Reflection;
using C0deGeek.ObjectCompare.Collections;
using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Comparison.Exceptions;
using C0deGeek.ObjectCompare.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace C0deGeek.ObjectCompare.Metadata;

/// <summary>
/// Provides comparison functionality based on type metadata
/// </summary>
public class MetadataComparer(ComparisonConfig config) : IMetadataComparer
{
    private readonly ComparisonConfig _config = Guard.ThrowIfNull(config, nameof(config));
    private readonly ILogger _logger = config.Logger ?? NullLogger.Instance;
    private readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();

    public bool CompareWithMetadata(object obj1, object obj2, TypeMetadata metadata, 
        string path, ComparisonResult result)
    {
        try
        {
            if (metadata.IsSimpleType)
            {
                return CompareSimpleTypes(obj1, obj2, metadata, path, result);
            }

            if (metadata.IsCollection)
            {
                return CompareCollections(obj1, obj2, metadata, path, result);
            }

            return CompareComplexTypes(obj1, obj2, metadata, path, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing objects with metadata at path {Path}", path);
            throw new ComparisonException("Error comparing objects with metadata", path, ex);
        }
    }

    private bool CompareSimpleTypes(object obj1, object obj2, TypeMetadata metadata, 
        string path, ComparisonResult result)
    {
        if (metadata is { HasCustomEquality: true, EqualityComparer: not null })
        {
            if (!metadata.EqualityComparer(obj1, obj2))
            {
                result.AddDifference($"Values differ: {obj1} != {obj2}", path);
                return false;
            }
            return true;
        }

        if (!obj1.Equals(obj2))
        {
            result.AddDifference($"Values differ: {obj1} != {obj2}", path);
            return false;
        }

        return true;
    }

    private ICollectionComparer GetCollectionComparer()
    {
        return _config.IgnoreCollectionOrder 
            ? new UnorderedCollectionComparer(_config)
            : new OrderedCollectionComparer(_config);
    }
    
    private bool CompareCollections(object obj1, object obj2, TypeMetadata metadata, 
        string path, ComparisonResult result)
    {
        var collection1 = (IEnumerable)obj1;
        var collection2 = (IEnumerable)obj2;

        var comparer = GetCollectionComparer();

        return comparer.CompareCollections(collection1, collection2, path, result);
    }

    private bool CompareComplexTypes(object obj1, object obj2, TypeMetadata metadata, 
        string path, ComparisonResult result)
    {
        var properties = GetCachedProperties(obj1.GetType());
        var isEqual = true;

        foreach (var prop in properties)
        {
            if (!ShouldCompareProperty(prop)) continue;

            var propertyPath = $"{path}.{prop.Name}";
            var value1 = prop.GetValue(obj1);
            var value2 = prop.GetValue(obj2);

            var propertyMetadata = TypeCache.GetMetadata(
                prop.PropertyType, _config.UseCachedMetadata);

            if (!CompareWithMetadata(value1!, value2!, propertyMetadata, propertyPath, result))
            {
                isEqual = false;
                if (!_config.ContinueOnDifference)
                {
                    break;
                }
            }
        }

        return isEqual;
    }

    private PropertyInfo[] GetCachedProperties(Type type)
    {
        return _propertyCache.GetOrAdd(type, t => t.GetProperties());
    }

    private bool ShouldCompareProperty(PropertyInfo prop)
    {
        if (_config.ExcludedProperties.Contains(prop.Name)) return false;
        if (!prop.CanRead) return false;
        if (!_config.CompareReadOnlyProperties && !prop.CanWrite) return false;
        return true;
    }
}