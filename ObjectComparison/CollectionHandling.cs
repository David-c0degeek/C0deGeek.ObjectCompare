using System.Collections;

namespace ObjectComparison;

/// <summary>
/// Specialized collection handling utilities
/// </summary>
internal static class CollectionHandling
{
    public static object CloneCollection(Type collectionType, IEnumerable source,
        Func<object, object> elementCloner)
    {
        // Handle arrays
        if (collectionType.IsArray)
        {
            return CloneArray(collectionType, source, elementCloner);
        }

        // Handle dictionaries
        if (IsDictionary(collectionType))
        {
            return CloneDictionary(collectionType, source, elementCloner);
        }

        // Handle sets
        if (IsSet(collectionType))
        {
            return CloneSet(collectionType, source, elementCloner);
        }

        // Handle queues and stacks
        if (IsQueueOrStack(collectionType))
        {
            return CloneQueueOrStack(collectionType, source, elementCloner);
        }

        // Default to List<T> for other collection types
        return CloneGenericList(collectionType, source, elementCloner);
    }

    private static object CloneArray(Type arrayType, IEnumerable source,
        Func<object, object> elementCloner)
    {
        var elementType = arrayType.GetElementType();
        var sourceArray = source.Cast<object>().ToArray();
        var array = Array.CreateInstance(elementType, sourceArray.Length);

        for (var i = 0; i < sourceArray.Length; i++)
        {
            array.SetValue(elementCloner(sourceArray[i]), i);
        }

        return array;
    }

    private static object CloneDictionary(Type dictType, IEnumerable source,
        Func<object, object> elementCloner)
    {
        var genericArgs = dictType.GetGenericArguments();
        var keyType = genericArgs[0];
        var valueType = genericArgs[1];

        var dictType1 = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        var dict = Activator.CreateInstance(dictType1);
        var addMethod = dictType1.GetMethod("Add");

        foreach (dynamic entry in source)
        {
            var clonedKey = elementCloner(entry.Key);
            var clonedValue = elementCloner(entry.Value);
            addMethod.Invoke(dict, new[] { clonedKey, clonedValue });
        }

        return dict;
    }

    private static object CloneSet(Type setType, IEnumerable source,
        Func<object, object> elementCloner)
    {
        var elementType = setType.GetGenericArguments()[0];
        var hashSetType = typeof(HashSet<>).MakeGenericType(elementType);
        var set = Activator.CreateInstance(hashSetType);
        var addMethod = hashSetType.GetMethod("Add");

        foreach (var item in source)
        {
            var clonedItem = elementCloner(item);
            addMethod.Invoke(set, new[] { clonedItem });
        }

        return set;
    }

    private static object CloneQueueOrStack(Type collectionType, IEnumerable source,
        Func<object, object> elementCloner)
    {
        var elementType = collectionType.GetGenericArguments()[0];
        var items = source.Cast<object>().Select(elementCloner).ToList();

        if (collectionType.GetGenericTypeDefinition() == typeof(Queue<>))
        {
            var queueType = typeof(Queue<>).MakeGenericType(elementType);
            var queue = Activator.CreateInstance(queueType);
            var enqueueMethod = queueType.GetMethod("Enqueue");

            foreach (var item in items)
            {
                enqueueMethod.Invoke(queue, new[] { item });
            }

            return queue;
        }
        else // Stack<T>
        {
            var stackType = typeof(Stack<>).MakeGenericType(elementType);
            var stack = Activator.CreateInstance(stackType);
            var pushMethod = stackType.GetMethod("Push");

            // Push in reverse order to maintain original order
            foreach (var item in items.AsEnumerable().Reverse())
            {
                pushMethod.Invoke(stack, new[] { item });
            }

            return stack;
        }
    }

    private static object CloneGenericList(Type collectionType, IEnumerable source,
        Func<object, object> elementCloner)
    {
        var elementType = GetElementType(collectionType);
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = Activator.CreateInstance(listType);
        var addMethod = listType.GetMethod("Add");

        foreach (var item in source)
        {
            var clonedItem = elementCloner(item);
            addMethod.Invoke(list, new[] { clonedItem });
        }

        // If the original type was a List<T>, return as is
        if (IsGenericList(collectionType))
        {
            return list;
        }

        // Try to convert to the original collection type
        try
        {
            var constructor = collectionType.GetConstructor(
                new[] { typeof(IEnumerable<>).MakeGenericType(elementType) });

            if (constructor != null)
            {
                return constructor.Invoke(new[] { list });
            }
        }
        catch
        {
            // Fall back to list if conversion fails
        }

        return list;
    }

    private static bool IsDictionary(Type type)
    {
        return type.IsGenericType &&
               (type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
                type.GetGenericTypeDefinition() == typeof(IDictionary<,>));
    }

    private static bool IsSet(Type type)
    {
        return type.IsGenericType &&
               (type.GetGenericTypeDefinition() == typeof(HashSet<>) ||
                type.GetGenericTypeDefinition() == typeof(ISet<>));
    }

    private static bool IsQueueOrStack(Type type)
    {
        return type.IsGenericType &&
               (type.GetGenericTypeDefinition() == typeof(Queue<>) ||
                type.GetGenericTypeDefinition() == typeof(Stack<>));
    }

    private static bool IsGenericList(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(List<>);
    }

    private static Type GetElementType(Type collectionType)
    {
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType();
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
}