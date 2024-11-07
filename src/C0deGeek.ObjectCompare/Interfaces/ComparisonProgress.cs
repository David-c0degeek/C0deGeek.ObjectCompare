namespace C0deGeek.ObjectCompare.Interfaces;

/// <summary>
/// Represents the progress of a comparison operation
/// </summary>
public class ComparisonProgress
{
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int Differences { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public double PercentageComplete => 
        TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;
}