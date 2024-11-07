using System.Runtime.CompilerServices;
using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Comparison.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace C0deGeek.ObjectCompare.Cloning;

/// <summary>
/// Base class for clone strategies providing common functionality
/// </summary>
public abstract class CloneStrategyBase(ILogger? logger = null) : ICloneStrategy
{
    protected readonly ILogger Logger = logger ?? NullLogger.Instance;

    public abstract bool CanHandle(Type type);
    
    public abstract object? Clone(object? obj, CloneContext context);
    
    public abstract int Priority { get; }

    protected void LogCloning(string strategyName, Type? type)
    {
        Logger.LogDebug(
            "Using {Strategy} to clone object of type {Type}",
            strategyName,
            type?.Name ?? "null");
    }

    protected static object CreateInstance(Type type)
    {
        try
        {
            // Handle arrays separately
            if (type.IsArray)
            {
                var elementType = type.GetElementType() ?? throw new ArgumentException($"Could not get element type for array type {type.Name}");
                return Array.CreateInstance(elementType, 0); // Create an empty array
            }
            
            // Try to get the parameterless constructor
            var constructor = type.GetConstructor(Type.EmptyTypes);
            return constructor is not null
                ? Activator.CreateInstance(type)!
                // If no parameterless constructor exists, use alternative instantiation
                : RuntimeHelpers.GetUninitializedObject(type);
        }
        catch (Exception ex)
        {
            throw new ComparisonException(
                ExceptionHelper.CreateCloneFailureMessage(type), "", ex);
        }
    }
}