using System.Diagnostics;
using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Comparison.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace C0deGeek.ObjectCompare.Performance;

/// <summary>
/// Provides benchmarking capabilities for comparison operations
/// </summary>
public class BenchmarkRunner
{
    private readonly ILogger _logger;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly MetricsCollector _metricsCollector;
    private readonly BenchmarkConfig _config;

    public BenchmarkRunner(BenchmarkConfig config, ILogger? logger = null)
    {
        _config = Guard.ThrowIfNull(config, nameof(config));
        _logger = logger ?? NullLogger.Instance;
        _performanceMonitor = new PerformanceMonitor(_logger);
        _metricsCollector = new MetricsCollector(_logger, TimeSpan.FromMinutes(1));
    }

    public async Task<BenchmarkResult> RunBenchmarkAsync<T>(
        string name,
        Func<T, T, Task<ComparisonResult>> comparisonFunc,
        T obj1,
        T obj2,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting benchmark: {Name}", name);

        var result = new BenchmarkResult
        {
            Name = name,
            StartTime = DateTime.UtcNow
        };

        try
        {
            await RunWarmup(comparisonFunc, obj1, obj2, cancellationToken);
            await RunIterations(comparisonFunc, obj1, obj2, result, cancellationToken);
            
            result.EndTime = DateTime.UtcNow;
            result.Success = true;

            LogBenchmarkResults(result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Benchmark {Name} failed", name);
            result.Success = false;
            result.Error = ex.Message;
            throw;
        }
    }

    private async Task RunWarmup<T>(
        Func<T, T, Task<ComparisonResult>> comparisonFunc,
        T obj1,
        T obj2,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Running warmup iterations");

        for (var i = 0; i < _config.WarmupIterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await comparisonFunc(obj1, obj2);
        }

        // Force GC collection after warmup
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private async Task RunIterations<T>(
        Func<T, T, Task<ComparisonResult>> comparisonFunc,
        T obj1,
        T obj2,
        BenchmarkResult result,
        CancellationToken cancellationToken)
    {
        var iterations = new List<IterationResult>();
        var memoryBefore = GC.GetTotalMemory(true);

        for (var i = 0; i < _config.Iterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var operation = _performanceMonitor.TrackOperation($"Iteration_{i}");
            var iterationResult = await RunSingleIteration(comparisonFunc, obj1, obj2);
            iterations.Add(iterationResult);

            if (_config.DelayBetweenIterations > TimeSpan.Zero)
            {
                await Task.Delay(_config.DelayBetweenIterations, cancellationToken);
            }
        }

        var memoryAfter = GC.GetTotalMemory(true);

        result.Iterations = iterations;
        result.MemoryUsed = memoryAfter - memoryBefore;
        result.AverageTime = TimeSpan.FromMilliseconds(
            iterations.Average(i => i.Duration.TotalMilliseconds));
        result.MedianTime = CalculateMedianTime(iterations);
        result.MinTime = iterations.Min(i => i.Duration);
        result.MaxTime = iterations.Max(i => i.Duration);
        result.Percentile95 = CalculatePercentile(iterations, 95);
        result.Percentile99 = CalculatePercentile(iterations, 99);
    }

    private static async Task<IterationResult> RunSingleIteration<T>(
        Func<T, T, Task<ComparisonResult>> comparisonFunc,
        T obj1,
        T obj2)
    {
        var sw = Stopwatch.StartNew();
        var comparisonResult = await comparisonFunc(obj1, obj2);
        sw.Stop();

        return new IterationResult
        {
            Duration = sw.Elapsed,
            ComparisonResult = comparisonResult,
            MemoryUsed = GC.GetTotalMemory(false)
        };
    }

    private static TimeSpan CalculateMedianTime(List<IterationResult> iterations)
    {
        var sortedTimes = iterations
            .Select(i => i.Duration.TotalMilliseconds)
            .OrderBy(t => t)
            .ToList();

        var mid = sortedTimes.Count / 2;
        return TimeSpan.FromMilliseconds(
            sortedTimes.Count % 2 == 0
                ? (sortedTimes[mid - 1] + sortedTimes[mid]) / 2
                : sortedTimes[mid]);
    }

    private static TimeSpan CalculatePercentile(
        List<IterationResult> iterations, int percentile)
    {
        var sortedTimes = iterations
            .Select(i => i.Duration.TotalMilliseconds)
            .OrderBy(t => t)
            .ToList();

        var index = (int)Math.Ceiling(percentile / 100.0 * sortedTimes.Count) - 1;
        return TimeSpan.FromMilliseconds(
            sortedTimes[Math.Max(0, index)]);
    }

    private void LogBenchmarkResults(BenchmarkResult result)
    {
        _logger.LogInformation(
            "Benchmark {Name} completed. Average: {Average}ms, " +
            "Median: {Median}ms, Min: {Min}ms, Max: {Max}ms, " +
            "Memory: {Memory}bytes",
            result.Name,
            result.AverageTime.TotalMilliseconds,
            result.MedianTime.TotalMilliseconds,
            result.MinTime.TotalMilliseconds,
            result.MaxTime.TotalMilliseconds,
            result.MemoryUsed);
    }
}