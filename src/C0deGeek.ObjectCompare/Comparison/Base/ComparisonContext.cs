using System.Diagnostics;
using C0deGeek.ObjectCompare.Models;

namespace C0deGeek.ObjectCompare.Comparison.Base;

/// <summary>
/// Maintains state and context during comparison operations
/// </summary>
public class ComparisonContext
{
    private readonly HashSet<string> _processedTypes = new();
    private readonly object _lock = new();
    private int _objectsCompared;
    private int _currentDepth;

    public IReadOnlyCollection<string> ComparisonPath => _comparisonStack.Select(o => o.ToString() ?? "").ToList();
    private readonly Stack<object> _comparisonStack = new();

    public HashSet<ComparisonPair> ComparedObjects { get; } = [];
    public Stopwatch Timer { get; } = new();
    public int MaxDepthReached { get; private set; }

    public int CurrentDepth
    {
        get => _currentDepth;
        set
        {
            _currentDepth = value;
            MaxDepthReached = Math.Max(MaxDepthReached, value);
        }
    }

    public int ObjectsCompared
    {
        get
        {
            lock (_lock)
            {
                return _objectsCompared;
            }
        }
    }

    public bool AddProcessedType(string typeKey)
    {
        lock (_lock)
        {
            return _processedTypes.Add(typeKey);
        }
    }

    public void RemoveProcessedType(string typeKey)
    {
        lock (_lock)
        {
            _processedTypes.Remove(typeKey);
        }
    }

    public void IncrementObjectCount()
    {
        lock (_lock)
        {
            _objectsCompared++;
        }
    }

    public void PushObject(object obj)
    {
        _comparisonStack.Push(obj);
        CurrentDepth = _comparisonStack.Count;
    }

    public void PopObject()
    {
        if (_comparisonStack.Count > 0)
        {
            _comparisonStack.Pop();
            CurrentDepth = _comparisonStack.Count;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _objectsCompared = 0;
            _processedTypes.Clear();
            _comparisonStack.Clear();
            ComparedObjects.Clear();
            MaxDepthReached = 0;
            CurrentDepth = 0;
            Timer.Reset();
        }
    }
}