using C0deGeek.ObjectCompare.Configuration;

namespace C0deGeek.ObjectCompare.Enums;

/// <summary>
/// Provides extension methods for performance levels
/// </summary>
public static class PerformanceLevelExtensions
{
    public static PerformanceConfiguration ToConfiguration(
        this PerformanceLevel level)
    {
        var config = new PerformanceConfiguration();

        switch (level)
        {
            case PerformanceLevel.None:
                config.EnableMetrics = false;
                config.EnableParallelProcessing = false;
                config.TrackMemoryUsage = false;
                break;

            case PerformanceLevel.Basic:
                config.EnableMetrics = true;
                config.EnableParallelProcessing = false;
                config.BatchSize = 100;
                break;

            case PerformanceLevel.Moderate:
                config.EnableMetrics = true;
                config.EnableParallelProcessing = true;
                config.BatchSize = 250;
                config.MaxDegreeOfParallelism = Environment.ProcessorCount;
                break;

            case PerformanceLevel.Aggressive:
                config.EnableMetrics = true;
                config.EnableParallelProcessing = true;
                config.BatchSize = 500;
                config.MaxDegreeOfParallelism = Environment.ProcessorCount * 2;
                config.EnableThreadPoolOptimization = true;
                break;

            case PerformanceLevel.Maximum:
                config.EnableMetrics = false;
                config.EnableParallelProcessing = true;
                config.BatchSize = 1000;
                config.MaxDegreeOfParallelism = Environment.ProcessorCount * 4;
                config.EnableThreadPoolOptimization = true;
                config.ResourceManagement.EnableResourcePooling = true;
                break;

            case PerformanceLevel.MemoryOptimized:
                config.EnableMetrics = true;
                config.TrackMemoryUsage = true;
                config.BatchSize = 50;
                config.ResourceManagement.MaxMemoryUsage = 512 * 1024 * 1024; // 512MB
                config.Threading.MaxThreads = Environment.ProcessorCount;
                break;

            case PerformanceLevel.SpeedOptimized:
                config.EnableMetrics = false;
                config.EnableParallelProcessing = true;
                config.BatchSize = 2000;
                config.MaxDegreeOfParallelism = -1; // Unlimited
                config.EnableThreadPoolOptimization = true;
                config.Threading.EnableWorkStealing = true;
                break;

            case PerformanceLevel.Balanced:
                config.EnableMetrics = true;
                config.EnableParallelProcessing = true;
                config.BatchSize = 250;
                config.MaxDegreeOfParallelism = Environment.ProcessorCount * 2;
                config.ResourceManagement.EnableResourcePooling = true;
                config.Threading.EnableWorkStealing = true;
                break;
        }

        return config;
    }

    public static bool IsOptimized(this PerformanceLevel level) =>
        level >= PerformanceLevel.Moderate;

    public static bool RequiresMonitoring(this PerformanceLevel level) =>
        level != PerformanceLevel.None && 
        level != PerformanceLevel.Maximum && 
        level != PerformanceLevel.SpeedOptimized;

    public static bool SupportsParallel(this PerformanceLevel level) =>
        level >= PerformanceLevel.Moderate;
}