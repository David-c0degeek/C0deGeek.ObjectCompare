namespace C0deGeek.ObjectCompare.Performance;

public class MemoryMetrics
{
    public long WorkingSet { get; init; }
    public long PrivateMemory { get; init; }
    public long ManagedMemory { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
}