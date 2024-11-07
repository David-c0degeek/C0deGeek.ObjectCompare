using System.Collections.Concurrent;
using System.Reflection;
using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Comparison.Exceptions;
using C0deGeek.ObjectCompare.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace C0deGeek.ObjectCompare.ValueObjects;

/// <summary>
/// Provides flexible comparison functionality for value objects with runtime type resolution
/// </summary>
public class FlexibleValueObjectComparer(double precision = 1e-10, ILogger? logger = null) : ICustomComparer
{
    private readonly ConcurrentDictionary<Type, MethodInfo?> _precisionComparerCache = new();
    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    public virtual bool AreEqual(object? obj1, object? obj2, ComparisonConfig config)  // Add virtual and nullable
    {
        if (obj1 is not ValueObject valueObj1 || obj2 is not ValueObject valueObj2)
            return false;

        var type = obj1.GetType();

        try
        {
            // Try to find a precision comparer first
            var precisionComparer = GetPrecisionComparer(type);
            if (precisionComparer != null)
            {
                return (bool)precisionComparer.Invoke(null, [valueObj1, valueObj2, precision])!;
            }

            // Fall back to standard equality
            return valueObj1.Equals(valueObj2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error comparing value objects of type {Type}", type.Name);
            throw new ComparisonException(
                $"Error comparing value objects of type {type.Name}", "", ex);
        }
    }

    private MethodInfo? GetPrecisionComparer(Type type)
    {
        return _precisionComparerCache.GetOrAdd(type, t =>
        {
            // Look for a method named "ArePrecisionEqual" with the correct signature
            return t.GetMethod("ArePrecisionEqual",
                BindingFlags.Public | BindingFlags.Static,
                [t, t, typeof(double)]);
        });
    }

    /// <summary>
    /// Creates a flexible comparer optimized for the given value object type
    /// </summary>
    public static FlexibleValueObjectComparer CreateFor<T>(double precision = 1e-10) 
        where T : ValueObject
    {
        return new FlexibleValueObjectComparer(precision);
    }

    /// <summary>
    /// Creates a flexible comparer with custom comparison logic
    /// </summary>
    public static FlexibleValueObjectComparer CreateWithCustomLogic(
        Func<ValueObject, ValueObject, bool> compareFunc,
        double precision = 1e-10)
    {
        return new CustomLogicValueObjectComparer(compareFunc, precision);
    }

    private class CustomLogicValueObjectComparer(
        Func<ValueObject, ValueObject, bool> compareFunc,
        double precision)
        : FlexibleValueObjectComparer(precision)
    {
        private readonly Func<ValueObject, ValueObject, bool> _compareFunc = 
            Guard.ThrowIfNull(compareFunc, nameof(compareFunc));

        public override bool AreEqual(object? obj1, object? obj2, ComparisonConfig config)  // Match parameter nullability
        {
            if (obj1 is not ValueObject valueObj1 || obj2 is not ValueObject valueObj2)
                return false;

            return _compareFunc(valueObj1, valueObj2);
        }
    }
}