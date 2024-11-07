using System.Collections.Concurrent;
using System.Linq.Expressions;
using C0deGeek.ObjectCompare.Comparison.Exceptions;

namespace C0deGeek.ObjectCompare.Collections;

/// <summary>
/// Provides specialized collection handling utilities with performance optimizations
/// </summary>
public class CollectionHandling
{
    private static readonly ConcurrentDictionary<Type, Type> ElementTypeCache = new();
    private static readonly ConcurrentDictionary<Type, Func<object[], Array>> ArrayCreatorCache = new();

   public object CloneCollection(Type collectionType, IEnumerable source, Func<object, object> elementCloner)
    {
        ArgumentNullException.ThrowIfNull(collectionType);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(elementCloner);

        return collectionType.IsArray ? CloneArray(collectionType, source, elementCloner)
            : IsDictionary(collectionType) ? CloneDictionary(collectionType, source, elementCloner)
            : IsSet(collectionType) ? CloneSet(collectionType, source, elementCloner)
            : IsQueueOrStack(collectionType) ? CloneQueueOrStack(collectionType, source, elementCloner)
            : CloneGenericList(collectionType, source, elementCloner);
    }

    private static Array CloneArray(Type arrayType, IEnumerable source, 
        Func<object, object> elementCloner)
    {
        var elementType = arrayType.GetElementType() ?? 
            throw new ArgumentException(
                $"Could not get element type for array type {arrayType.Name}");
            
        var sourceArray = source.Cast<object>().ToArray();
        var arrayCreator = GetOrCreateArrayCreator(elementType);
        var array = arrayCreator([sourceArray.Length]);

        try
        {
            for (var i = 0; i < sourceArray.Length; i++)
            {
                var clonedElement = elementCloner(sourceArray[i]);
                if (clonedElement is null && !elementType.IsClass)
                {
                    throw new InvalidOperationException(
                        $"Cannot assign null to array element of type {elementType.Name}");
                }
                array.SetValue(clonedElement, i);
            }
        }
        catch (Exception ex)
        {
            throw new ComparisonException(
                $"Failed to clone array of type {arrayType.Name}", "", ex);
        }

        return array;
    }

    private static Func<object[], Array> GetOrCreateArrayCreator(Type elementType)
    {
        return ArrayCreatorCache.GetOrAdd(elementType, type =>
        {
            var lengthParam = Expression.Parameter(typeof(object[]), "args");
            var lengthArg = Expression.Convert(
                Expression.ArrayIndex(lengthParam, Expression.Constant(0)),
                typeof(int));
            
            var newArrayExpr = Expression.NewArrayBounds(
                elementType,
                lengthArg);

            return Expression.Lambda<Func<object[], Array>>(
                newArrayExpr, lengthParam).Compile();
        });
    }

    private static object CloneDictionary(Type dictType, IEnumerable source, 
        Func<object, object> elementCloner)
    {
        var genericArgs = dictType.GetGenericArguments();
        if (genericArgs.Length != 2)
        {
            throw new ArgumentException(
                $"Dictionary type {dictType.Name} must have exactly two generic arguments");
        }

        var keyType = genericArgs[0];
        var valueType = genericArgs[1];
        var dictType1 = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        var dict = Activator.CreateInstance(dictType1) ?? 
            throw new InvalidOperationException(
                $"Failed to create dictionary of type {dictType1.Name}");
            
        var addMethod = dictType1.GetMethod("Add") ?? 
            throw new InvalidOperationException(
                $"Could not find Add method on dictionary type {dictType1.Name}");

        try
        {
            foreach (dynamic entry in source)
            {
                var clonedKey = elementCloner(entry.Key);
                var clonedValue = elementCloner(entry.Value);
                addMethod.Invoke(dict, [clonedKey, clonedValue]);
            }
        }
        catch (Exception ex)
        {
            throw new ComparisonException(
                $"Failed to clone dictionary of type {dictType.Name}", "", ex);
        }

        return dict;
    }

    private static object CloneSet(Type setType, IEnumerable source, 
        Func<object, object> elementCloner)
    {
        var elementType = setType.GetGenericArguments().FirstOrDefault() ??
            throw new ArgumentException(
                $"Set type {setType.Name} must have a generic argument");

        var hashSetType = typeof(HashSet<>).MakeGenericType(elementType);
        var set = Activator.CreateInstance(hashSetType) ??
            throw new InvalidOperationException(
                $"Failed to create set of type {hashSetType.Name}");

        var addMethod = hashSetType.GetMethod("Add") ??
            throw new InvalidOperationException(
                $"Could not find Add method on set type {hashSetType.Name}");

        try
        {
            foreach (var item in source)
            {
                var clonedItem = elementCloner(item);
                addMethod.Invoke(set, [clonedItem]);
            }
        }
        catch (Exception ex)
        {
            throw new ComparisonException(
                $"Failed to clone set of type {setType.Name}", "", ex);
        }

        return set;
    }

    private static object CloneQueueOrStack(Type collectionType, IEnumerable source, 
        Func<object, object> elementCloner)
    {
        var elementType = collectionType.GetGenericArguments().FirstOrDefault() ??
            throw new ArgumentException(
                $"Collection type {collectionType.Name} must have a generic argument");

        var items = source.Cast<object>().Select(elementCloner).ToList();
        var genericType = collectionType.GetGenericTypeDefinition();

        if (genericType == typeof(Queue<>))
        {
            return CloneQueue(elementType, items);
        }
        
        if (genericType == typeof(Stack<>))
        {
            return CloneStack(elementType, items);
        }

        throw new ArgumentException(
            $"Unsupported collection type: {collectionType.Name}");
    }

    private static object CloneQueue(Type elementType, IEnumerable<object> items)
    {
        var queueType = typeof(Queue<>).MakeGenericType(elementType);
        var queue = Activator.CreateInstance(queueType) ??
            throw new InvalidOperationException(
                $"Failed to create queue of type {queueType.Name}");

        var enqueueMethod = queueType.GetMethod("Enqueue") ??
            throw new InvalidOperationException(
                $"Could not find Enqueue method on queue type {queueType.Name}");

        try
        {
            foreach (var item in items)
            {
                enqueueMethod.Invoke(queue, [item]);
            }
        }
        catch (Exception ex)
        {
            throw new ComparisonException(
                $"Failed to clone queue of type {queueType.Name}", "", ex);
        }

        return queue;
    }

    private static object CloneStack(Type elementType, IEnumerable<object> items)
    {
        var stackType = typeof(Stack<>).MakeGenericType(elementType);
        var stack = Activator.CreateInstance(stackType) ??
            throw new InvalidOperationException(
                $"Failed to create stack of type {stackType.Name}");

        var pushMethod = stackType.GetMethod("Push") ??
            throw new InvalidOperationException(
                $"Could not find Push method on stack type {stackType.Name}");

        try
        {
            foreach (var item in items.Reverse())
            {
                pushMethod.Invoke(stack, [item]);
            }
        }
        catch (Exception ex)
        {
            throw new ComparisonException(
                $"Failed to clone stack of type {stackType.Name}", "", ex);
        }

        return stack;
    }

    private static object CloneGenericList(Type collectionType, IEnumerable source, 
        Func<object, object> elementCloner)
    {
        var elementType = GetElementType(collectionType);
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = Activator.CreateInstance(listType) ??
            throw new InvalidOperationException(
                $"Failed to create list of type {listType.Name}");

        var addMethod = listType.GetMethod("Add") ??
            throw new InvalidOperationException(
                $"Could not find Add method on list type {listType.Name}");

        try
        {
            foreach (var item in source)
            {
                var clonedItem = elementCloner(item);
                addMethod.Invoke(list, [clonedItem]);
            }

            // If the original type was a List<T>, return as is
            if (IsGenericList(collectionType))
            {
                return list;
            }

            // Try to convert to the original collection type
            var constructor = collectionType.GetConstructor(
                [typeof(IEnumerable<>).MakeGenericType(elementType)]);

            return constructor?.Invoke([list]) ?? list;
        }
        catch (Exception ex)
        {
            throw new ComparisonException(
                $"Failed to clone list of type {collectionType.Name}", "", ex);
        }
    }

    private static bool IsDictionary(Type type)
    {
        return type.IsGenericType && (
            type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
            type.GetGenericTypeDefinition() == typeof(IDictionary<,>));
    }

    private static bool IsSet(Type type)
    {
        return type.IsGenericType && (
            type.GetGenericTypeDefinition() == typeof(HashSet<>) ||
            type.GetGenericTypeDefinition() == typeof(ISet<>));
    }

    private static bool IsQueueOrStack(Type type)
    {
        return type.IsGenericType && (
            type.GetGenericTypeDefinition() == typeof(Queue<>) ||
            type.GetGenericTypeDefinition() == typeof(Stack<>));
    }

    private static bool IsGenericList(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(List<>);
    }

    private static Type GetElementType(Type collectionType)
    {
        return ElementTypeCache.GetOrAdd(collectionType, type =>
        {
            if (type.IsArray)
            {
                return type.GetElementType() ?? typeof(object);
            }

            if (type.IsGenericType)
            {var genericArgs = type.GetGenericArguments();
                if (genericArgs.Length == 1)
                {
                    return genericArgs[0];
                }
            }

            var enumType = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                                   i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            return enumType?.GetGenericArguments()[0] ?? typeof(object);
        });
    }

    /// <summary>
    /// Provides optimized collection comparison for simple types
    /// </summary>
    public static bool FastCompareCollections(IEnumerable collection1, IEnumerable collection2)
    {
        var list1 = collection1.Cast<object>().ToList();
        var list2 = collection2.Cast<object>().ToList();

        if (list1.Count != list2.Count)
        {
            return false;
        }

        // For small collections, use direct comparison
        if (list1.Count <= 10)
        {
            return CompareSmallCollections(list1, list2);
        }

        // For larger collections, use dictionary-based comparison
        return CompareLargeCollections(list1, list2);
    }

    private static bool CompareSmallCollections(List<object> list1, List<object> list2)
    {
        var used = new bool[list2.Count];
        
        foreach (var item1 in list1)
        {
            var found = false;
            for (var j = 0; j < list2.Count; j++)
            {
                if (used[j]) continue;
                
                if (!Equals(item1, list2[j])) continue;
                
                used[j] = true;
                found = true;
                break;
            }

            if (!found) return false;
        }

        return true;
    }

    private static bool CompareLargeCollections(List<object> list1, List<object> list2)
    {
        var counts = new Dictionary<object, int>(new FastEqualityComparer());

        // Count items in first list
        foreach (var item in list1)
        {
            if (!counts.ContainsKey(item))
            {
                counts[item] = 0;
            }
            counts[item]++;
        }

        // Compare with second list
        foreach (var item in list2)
        {
            if (!counts.ContainsKey(item) || counts[item] == 0)
            {
                return false;
            }
            counts[item]--;
        }

        // Verify all counts are zero
        return counts.Values.All(count => count == 0);
    }

    private class FastEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return x.Equals(y);
        }

        public int GetHashCode(object obj)
        {
            return obj?.GetHashCode() ?? 0;
        }
    }

    /// <summary>
    /// Creates a strongly typed collection of the specified type
    /// </summary>
    public static IEnumerable CreateTypedCollection(Type elementType, IEnumerable source)
    {
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType)!;

        foreach (var item in source)
        {
            list.Add(item);
        }

        return list;
    }

    /// <summary>
    /// Determines if two collections have the same element types
    /// </summary>
    public static bool HaveSameElementType(Type collection1, Type collection2)
    {
        var elementType1 = GetElementType(collection1);
        var elementType2 = GetElementType(collection2);

        return elementType1 == elementType2;
    }
}