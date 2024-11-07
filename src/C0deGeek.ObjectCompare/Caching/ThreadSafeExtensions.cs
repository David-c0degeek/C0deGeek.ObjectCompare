using System.Collections.Concurrent;

namespace C0deGeek.ObjectCompare.Caching;

internal static class ThreadSafeExtensions
{
    public static (TValue Value, bool Added) GetOrAddWithStatus<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TValue> valueFactory) where TKey : notnull
    {
        var added = false;
        var value = dictionary.GetOrAdd(key, k =>
        {
            added = true;
            return valueFactory(k);
        });
        return (value, added);
    }

    public static void AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            bag.Add(item);
        }
    }
}