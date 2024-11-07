namespace C0deGeek.ObjectCompare.Performance;

public class BenchmarkResult
{
    public string Name { get; init; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan AverageTime { get; set; }
    public TimeSpan MedianTime { get; set; }
    public TimeSpan MinTime { get; set; }
    public TimeSpan MaxTime { get; set; }
    public TimeSpan Percentile95 { get; set; }
    public TimeSpan Percentile99 { get; set; }
    public long MemoryUsed { get; set; }
    public List<IterationResult> Iterations { get; set; } = [];
    public bool Success { get; set; }
    public string? Error { get; set; }
    public TimeSpan TotalDuration => EndTime - StartTime;
}