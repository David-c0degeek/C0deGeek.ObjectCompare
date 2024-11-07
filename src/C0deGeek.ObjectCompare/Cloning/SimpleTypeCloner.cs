using Microsoft.Extensions.Logging;

namespace C0deGeek.ObjectCompare.Cloning;

/// <summary>
/// Strategy for cloning simple value types and immutable objects
/// </summary>
public class SimpleTypeCloner(ILogger? logger = null) : CloneStrategyBase(logger)
{
    private static readonly HashSet<Type> SimpleTypes =
    [
        typeof(byte), typeof(sbyte),
        typeof(short), typeof(ushort),
        typeof(int), typeof(uint),
        typeof(long), typeof(ulong),
        typeof(float), typeof(double),
        typeof(decimal), typeof(bool),
        typeof(char), typeof(string),
        typeof(DateTime), typeof(DateTimeOffset),
        typeof(TimeSpan), typeof(Guid)
    ];

    public override bool CanHandle(Type type)
    {
        return type.IsPrimitive || 
               type.IsEnum || 
               SimpleTypes.Contains(type) ||
               Nullable.GetUnderlyingType(type) != null;
    }

    public override int Priority => 100;

    public override object? Clone(object? obj, CloneContext context)
    {
        if (obj == null) return null;

        var type = obj.GetType();
        LogCloning(nameof(SimpleTypeCloner), type);

        // For nullable types, handle the underlying type
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return obj; // Nullable types are immutable
        }

        // Simple types can be returned as-is since they're immutable or value types
        return obj;
    }

    /// <summary>
    /// Determines if a type is a simple type that can be cloned by value
    /// </summary>
    public static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || 
               type.IsEnum || 
               SimpleTypes.Contains(type) ||
               Nullable.GetUnderlyingType(type) != null;
    }
}