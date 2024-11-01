using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace C0deGeek.ObjectCompare;

/// <summary>
/// Context for tracking comparison state
/// </summary>
internal class ComparisonContext
{
    public HashSet<ComparisonPair> ComparedObjects { get; } = [];
    public int CurrentDepth { get; set; }
    public Stopwatch Timer { get; } = new();
    public int ObjectsCompared { get; private set; }
    public int MaxDepthReached { get; private set; }
    private readonly Stack<object> _objectStack = new();

    public void PushObject(object obj)
    {
        _objectStack.Push(obj);
        ObjectsCompared++;
        MaxDepthReached = Math.Max(MaxDepthReached, _objectStack.Count);
    }

    public void PopObject()
    {
        if (_objectStack.Count > 0)
        {
            _objectStack.Pop();
        }
    }

    public readonly struct ComparisonPair(object obj1, object obj2) : IEquatable<ComparisonPair>
    {
        private readonly object _obj1 = obj1;
        private readonly object _obj2 = obj2;
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
    }
}