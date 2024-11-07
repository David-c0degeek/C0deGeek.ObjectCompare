namespace C0deGeek.ObjectCompare.Configuration;

/// <summary>
/// Provides configuration options for performance monitoring and optimization
/// </summary>
public class PerformanceConfiguration
{
    public bool EnableMetrics { get; set; } = true;
    public bool EnableParallelProcessing { get; set; }
    public int MaxDegreeOfParallelism { get; set; } = -1;
    public TimeSpan MetricsAggregationInterval { get; set; } = TimeSpan.FromMinutes(1);
    public bool TrackMemoryUsage { get; set; } = true;
    public bool EnableThreadPoolOptimization { get; set; } = true;
    public int BatchSize { get; set; } = 100;

    public ResourceManagementOptions ResourceManagement { get; set; } = new();
    public ThreadingOptions Threading { get; set; } = new();
    public MetricsOptions Metrics { get; set; } = new();

    public class ResourceManagementOptions
    {
        public long MaxMemoryUsage { get; set; } = 1024 * 1024 * 1024; // 1GB
        public int MaxConcurrentOperations { get; set; } = 100;
        public TimeSpan ResourceTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public bool EnableResourcePooling { get; set; } = true;
        public int PoolSize { get; set; } = 10;
    }

    public class ThreadingOptions
    {
        public bool UseThreadPool { get; set; } = true;
        public int MinThreads { get; set; } = 4;
        public int MaxThreads { get; set; } = 16;
        public TimeSpan ThreadTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public bool EnableWorkStealing { get; set; } = true;
    }

    public class MetricsOptions
    {
        public bool TrackDetailedMetrics { get; set; } = true;
        public TimeSpan MetricsRetention { get; set; } = TimeSpan.FromDays(1);
        public int MaxMetricsCount { get; set; } = 10000;
        public bool EnableHistograms { get; set; } = true;
        public bool TrackPerThreadMetrics { get; set; }
    }

    public PerformanceConfiguration Clone()
    {
        return new PerformanceConfiguration
        {
            EnableMetrics = EnableMetrics,
            EnableParallelProcessing = EnableParallelProcessing,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            MetricsAggregationInterval = MetricsAggregationInterval,
            TrackMemoryUsage = TrackMemoryUsage,
            EnableThreadPoolOptimization = EnableThreadPoolOptimization,
            BatchSize = BatchSize,
            ResourceManagement = new ResourceManagementOptions
            {
                MaxMemoryUsage = ResourceManagement.MaxMemoryUsage,
                MaxConcurrentOperations = ResourceManagement.MaxConcurrentOperations,
                ResourceTimeout = ResourceManagement.ResourceTimeout,
                EnableResourcePooling = ResourceManagement.EnableResourcePooling,
                PoolSize = ResourceManagement.PoolSize
            },
            Threading = new ThreadingOptions
            {
                UseThreadPool = Threading.UseThreadPool,
                MinThreads = Threading.MinThreads,
                MaxThreads = Threading.MaxThreads,
                ThreadTimeout = Threading.ThreadTimeout,
                EnableWorkStealing = Threading.EnableWorkStealing
            },
            Metrics = new MetricsOptions
            {
                TrackDetailedMetrics = Metrics.TrackDetailedMetrics,
                MetricsRetention = Metrics.MetricsRetention,
                MaxMetricsCount = Metrics.MaxMetricsCount,
                EnableHistograms = Metrics.EnableHistograms,
                TrackPerThreadMetrics = Metrics.TrackPerThreadMetrics
            }
        };
    }

    public class Builder
    {
        private readonly PerformanceConfiguration _config = new();

        public Builder WithParallelProcessing(bool enable = true)
        {
            _config.EnableParallelProcessing = enable;
            return this;
        }

        public Builder WithMaxDegreeOfParallelism(int maxDegree)
        {
            _config.MaxDegreeOfParallelism = maxDegree;return this;
        }

        public Builder WithMetricsAggregationInterval(TimeSpan interval)
        {
            _config.MetricsAggregationInterval = interval;
            return this;
        }

        public Builder WithBatchSize(int batchSize)
        {
            _config.BatchSize = batchSize;
            return this;
        }

        public Builder WithResourceManagement(Action<ResourceManagementOptions> configure)
        {
            configure(_config.ResourceManagement);
            return this;
        }

        public Builder WithThreading(Action<ThreadingOptions> configure)
        {
            configure(_config.Threading);
            return this;
        }

        public Builder WithMetrics(Action<MetricsOptions> configure)
        {
            configure(_config.Metrics);
            return this;
        }

        public PerformanceConfiguration Build()
        {
            return _config.Clone();
        }
    }
}