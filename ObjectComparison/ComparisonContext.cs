using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ObjectComparison;

/// <summary>
/// Context for tracking comparison state
/// </summary>
internal class ComparisonContext
{
    public HashSet<ComparisonPair> ComparedObjects { get; } = new();
    public int CurrentDepth { get; set; }
    public Stopwatch Timer { get; } = new();
    public int ObjectsCompared { get; set; }
    public int MaxDepthReached { get; set; }
    public readonly Stack<object> ObjectStack = new();

    public void PushObject(object obj)
    {
        ObjectStack.Push(obj);
        ObjectsCompared++;
        MaxDepthReached = Math.Max(MaxDepthReached, ObjectStack.Count);
    }

    public void PopObject()
    {
        if (ObjectStack.Count > 0)
        {
            ObjectStack.Pop();
        }
    }

    public readonly struct ComparisonPair : IEquatable<ComparisonPair>
    {
        private readonly object _obj1;
        private readonly object _obj2;
        private readonly int _hashCode;

        public ComparisonPair(object obj1, object obj2)
        {
            _obj1 = obj1;
            _obj2 = obj2;
            _hashCode = HashCode.Combine(
                RuntimeHelpers.GetHashCode(obj1),
                RuntimeHelpers.GetHashCode(obj2)
            );
        }

        public bool Equals(ComparisonPair other)
        {
            return ReferenceEquals(_obj1, other._obj1) &&
                   ReferenceEquals(_obj2, other._obj2);
        }

        public override bool Equals(object obj)
        {
            return obj is ComparisonPair other && Equals(other);
        }

        public override int GetHashCode() => _hashCode;
    }
}