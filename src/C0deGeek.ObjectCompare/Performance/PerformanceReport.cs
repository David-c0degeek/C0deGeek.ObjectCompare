namespace C0deGeek.ObjectCompare.Performance;

public class PerformanceReport
{
    public Dictionary<string, TimeSpan> OperationTimes { get; init; } = new();
    public Dictionary<string, long> ObjectCounts { get; init; } = new();
    public Dictionary<string, TimeSpan> AverageOperationTimes { get; init; } = new();
    public MemoryMetrics MemoryUsage { get; init; } = new();
    public double CpuUsage { get; init; }
}