namespace C0deGeek.ObjectCompare.Performance;

public record MetricValue
{
    public double Value { get; }
    public DateTime Timestamp { get; }

    public MetricValue(double value, DateTime timestamp = default)
    {
        Value = value;
        Timestamp = timestamp == default ? DateTime.UtcNow : timestamp;
    }
    
    public class MetricsReport
    {
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }
        public Dictionary<string, MetricsSummary> Metrics { get; init; } = new();
    }

    public class MetricsSummary
    {
        public int Count { get; init; }
        public double Min { get; init; }
        public double Max { get; init; }
        public double Average { get; init; }
        public double Median { get; init; }
        public double Percentile95 { get; init; }
        public double Percentile99 { get; init; }
    }
}