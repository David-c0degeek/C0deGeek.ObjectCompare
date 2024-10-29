using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace ObjectComparison;

/// <summary>
/// Metadata for a type including its properties, fields, and compiled accessors
/// </summary>
internal sealed class TypeMetadata
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

        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
        Properties = type.GetProperties(flags);
        Fields = type.GetFields(flags);
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
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
            type == typeof(TimeSpan) || type == typeof(Guid))
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
            .Concat(new[] { type })
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == EnumerableGenericType);

        return enumType?.GetGenericArguments().FirstOrDefault();
    }
}