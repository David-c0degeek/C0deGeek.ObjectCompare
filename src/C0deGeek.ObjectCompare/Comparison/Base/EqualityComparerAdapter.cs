namespace C0deGeek.ObjectCompare.Comparison.Base;

internal sealed class EqualityComparerAdapter<T>(IEqualityComparer<T> comparer) : IEqualityComparer
{
    private readonly IEqualityComparer<T> _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));

    public new bool Equals(object? x, object? y)
    {
        if (x == null && y == null) return true;
        if (x == null || y == null) return false;

        if (x is not T tX || y is not T tY)
        {
            throw new ArgumentException($"Objects must be of type {typeof(T).Name}");
        }

        return _comparer.Equals(tX, tY);
    }

    public int GetHashCode(object obj)
    {
        if (obj == null) return 0;
        if (obj is not T t)
        {
            throw new ArgumentException($"Object must be of type {typeof(T).Name}");
        }

        return _comparer.GetHashCode(t);
    }
}