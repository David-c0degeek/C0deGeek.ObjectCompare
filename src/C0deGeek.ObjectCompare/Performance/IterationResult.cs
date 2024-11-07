using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Performance;

public class IterationResult
{
    public TimeSpan Duration { get; init; }
    public ComparisonResult ComparisonResult { get; init; } = new();
    public long MemoryUsed { get; init; }
}