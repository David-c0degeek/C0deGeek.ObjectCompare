using System.Linq.Expressions;
using System.Reflection;

namespace C0deGeek.ObjectCompare.Metadata;

/// <summary>
/// Metadata for a type including its properties, fields, and compiled accessors
/// </summary>
public sealed class TypeMetadata
{
    private static readonly Type EnumerableGenericType = typeof(IEnumerable<>);

    public PropertyInfo[] Properties { get; }
    public FieldInfo[] Fields { get; }
    public bool IsSimpleType { get; }
    public Type? UnderlyingType { get; }
    public bool HasCustomEquality { get; }
    public Func<object, object, bool>? EqualityComparer { get; }
    public Type? ItemType { get; }
    public bool IsCollection { get; }

    public TypeMetadata(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        // For properties
        const BindingFlags propertyFlags = BindingFlags.Public | BindingFlags.Instance;
        Properties = type.GetProperties(propertyFlags);

        // For fields - get ALL fields including private ones
        const BindingFlags fieldFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Fields = type.GetFields(fieldFlags);
        
        IsSimpleType = IsSimpleTypeInternal(type);
        UnderlyingType = Nullable.GetUnderlyingType(type);
        HasCustomEquality = typeof(IEquatable<>).MakeGenericType(type).IsAssignableFrom(type);
        
        if (HasCustomEquality)
        {
            EqualityComparer = CreateEqualityComparer(type);
        }

        IsCollection = typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
        if (IsCollection)
        {
            ItemType = GetCollectionItemType(type);
        }
    }

    private static bool IsSimpleTypeInternal(Type type)
    {
        if (type.IsPrimitive 
            || type == typeof(string) 
            || type == typeof(decimal) 
            || type == typeof(DateTime) 
            || type == typeof(DateTimeOffset) 
            || type == typeof(TimeSpan) 
            || type == typeof(Guid)
            || type.IsEnum)
        {
            return true;
        }

        var typeCode = Type.GetTypeCode(type);
        return typeCode != TypeCode.Object;
    }

    private static Func<object, object, bool>? CreateEqualityComparer(Type type)
    {
        var method = type.GetMethod("Equals", [type]);
        if (method is null) return null;

        try
        {
            var param1 = Expression.Parameter(typeof(object), "x");
            var param2 = Expression.Parameter(typeof(object), "y");
            var typed1 = Expression.Convert(param1, type);
            var typed2 = Expression.Convert(param2, type);
            var equalCall = Expression.Call(typed1, method, typed2);

            return Expression.Lambda<Func<object, object, bool>>(equalCall, param1, param2).Compile();
        }
        catch (Exception)
        {
            // If expression compilation fails, return null to fall back to default comparison
            return null;
        }
    }

    private static Type? GetCollectionItemType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        var enumType = type.GetInterfaces()
            .Concat([type])
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == EnumerableGenericType);

        return enumType?.GetGenericArguments().FirstOrDefault();
    }
}