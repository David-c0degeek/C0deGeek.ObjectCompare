using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using C0deGeek.ObjectCompare.Common;

namespace C0deGeek.ObjectCompare.Comparison.Base;

/// <summary>
/// Maintains state and context during comparison operations
/// </summary>
public class ComparisonContext
{
    private readonly Stack<object> _objectStack = new();
    private readonly ConcurrentDictionary<string, object> _metadata = new();
    private int _currentDepth;

    public HashSet<ComparisonPair> ComparedObjects { get; } = [];
    public Stopwatch Timer { get; } = new();
    public int ObjectsCompared { get; set; }
    public int MaxDepthReached { get; set; }
    public IReadOnlyCollection<string> ComparisonPath => _objectStack.Select(o => o.ToString() ?? "").ToList();

    public int CurrentDepth
    {
        get => _currentDepth;
        set
        {
            _currentDepth = value;
            MaxDepthReached = Math.Max(MaxDepthReached, value);
        }
    }

    public void PushObject(object obj)
    {
        Guard.ThrowIfNull(obj, nameof(obj));
        
        _objectStack.Push(obj);
        ObjectsCompared++;
        CurrentDepth = _objectStack.Count;
    }

    public void PopObject()
    {
        if (_objectStack.Count > 0)
        {
            _objectStack.Pop();
            CurrentDepth = _objectStack.Count;
        }
    }

    public void SetMetadata<T>(string key, T value)
    {
        _metadata[key] = value!;
    }

    public T? GetMetadata<T>(string key)
    {
        return _metadata.TryGetValue(key, out var value) ? (T)value : default;
    }

    public readonly struct ComparisonPair(object obj1, object obj2) : IEquatable<ComparisonPair>
    {
        private readonly object _obj1 = Guard.ThrowIfNull(obj1, nameof(obj1));
        private readonly object _obj2 = Guard.ThrowIfNull(obj2, nameof(obj2));
        private readonly int _hashCode = HashCode.Combine(
            RuntimeHelpers.GetHashCode(obj1),
            RuntimeHelpers.GetHashCode(obj2)
        );

        public bool Equals(ComparisonPair other)
        {
            return ReferenceEquals(_obj1, other._obj1) &&
                   ReferenceEquals(_obj2, other._obj2);
        }

        public override bool Equals(object? obj)
        {
            return obj is ComparisonPair other && Equals(other);
        }

        public override int GetHashCode() => _hashCode;

        public static bool operator ==(ComparisonPair left, ComparisonPair right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ComparisonPair left, ComparisonPair right)
        {
            return !left.Equals(right);
        }
    }

    public void Reset()
    {
        _objectStack.Clear();
        _metadata.Clear();
        ComparedObjects.Clear();
        ObjectsCompared = 0;
        CurrentDepth = 0;
        MaxDepthReached = 0;
        Timer.Reset();
    }
}