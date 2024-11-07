using C0deGeek.ObjectCompare.Collections;
using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Comparison.Exceptions;
using Microsoft.Extensions.Logging;

namespace C0deGeek.ObjectCompare.Cloning;

/// <summary>
/// Strategy for cloning collections and arrays
/// </summary>
public class CollectionCloner(ComparisonConfig config, ILogger? logger = null) : CloneStrategyBase(logger)
{
    private readonly ComparisonConfig _config = Guard.ThrowIfNull(config, nameof(config));
    private readonly SimpleTypeCloner _simpleTypeCloner = new(logger);
    private readonly CollectionHandling _collectionHandling = new CollectionHandling();

    public override bool CanHandle(Type type)
    {
        return typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
    }

    public override int Priority => 50;

    public override object? Clone(object? obj, CloneContext context)
    {
        if (obj == null) return null;

        var type = obj.GetType();
        LogCloning(nameof(CollectionCloner), type);

        // Check for circular references
        if (context.TryGetExistingClone(obj, out var existingClone))
        {
            return existingClone;
        }

        try
        {
            var elementType = GetElementType(type);
            var cloneFunc = CreateElementCloneFunc(elementType, context);
            
            var clone = _collectionHandling.CloneCollection(type, (IEnumerable)obj, cloneFunc);
            context.RegisterClone(obj, clone);
            
            return clone;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cloning collection of type {Type}", type.Name);
            throw new ComparisonException(
                $"Error cloning collection of type {type.Name}", "", ex);
        }
    }

    private static Type GetElementType(Type collectionType)
    {
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType() ?? typeof(object);
        }

        if (collectionType.IsGenericType)
        {
            var genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length == 1)
            {
                return genericArgs[0];
            }
        }

        var enumType = collectionType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumType?.GetGenericArguments()[0] ?? typeof(object);
    }

    private Func<object, object?> CreateElementCloneFunc(Type elementType, CloneContext context)
    {
        // For simple types, return as-is
        if (_simpleTypeCloner.CanHandle(elementType))
        {
            return obj => obj;
        }

        // For complex types, use the complex type cloner
        var complexCloner = new ComplexTypeCloner(_config, Logger);
        return obj => complexCloner.Clone(obj, context);
    }
}