namespace C0deGeek.ObjectCompare.Performance;

public class BenchmarkConfig
{
    public int Iterations { get; set; } = 100;
    public int WarmupIterations { get; set; } = 5;
    public TimeSpan DelayBetweenIterations { get; set; } = TimeSpan.Zero;
    public bool CollectGarbage { get; set; } = true;
    public bool TrackMemory { get; set; } = true;
    public bool DetailedMetrics { get; set; } = false;
}