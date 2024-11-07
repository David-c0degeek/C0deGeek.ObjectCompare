using System.Collections.Concurrent;
using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Extensions;
using Microsoft.Extensions.Logging;

namespace C0deGeek.ObjectCompare.Performance;

/// <summary>
/// Collects and aggregates performance metrics
/// </summary>
public class MetricsCollector
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, MetricsSeries> _metrics = new();
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly TimeSpan _aggregationInterval;
    private readonly Timer _aggregationTimer;

    public MetricsCollector(ILogger logger, TimeSpan aggregationInterval)
    {
        _logger = Guard.ThrowIfNull(logger, nameof(logger));
        _performanceMonitor = new PerformanceMonitor(logger);
        _aggregationInterval = aggregationInterval;
        _aggregationTimer = new Timer(AggregateMetrics, null, 
            aggregationInterval, aggregationInterval);
    }

    public void RecordMetric(string name, double value)
    {
        _metrics.AddOrUpdate(name,
            _ => new MetricsSeries { Values = [new(value)] },
            (_, series) =>
            {
                series.Values.Add(new MetricValue(value));
                return series;
            });
    }

    public void RecordLatency(string operation, TimeSpan duration)
    {
        RecordMetric($"{operation}_latency", duration.TotalMilliseconds);
    }

    public void RecordError(string operation)
    {
        RecordMetric($"{operation}_errors", 1);
    }

    public void RecordObjectCount(string type, int count)
    {
        RecordMetric($"{type}_count", count);
    }

    public MetricValue.MetricsReport GenerateReport(TimeSpan window)
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime - window;

        var report = new MetricValue.MetricsReport
        {
            StartTime = startTime,
            EndTime = endTime,
            Metrics = _metrics.ToDictionary(
                kvp => kvp.Key,
                kvp => CalculateMetrics(kvp.Value, startTime, endTime))
        };

        _logger.LogInformation(
            "Generated metrics report for window {StartTime} to {EndTime}",
            startTime, endTime);

        return report;
    }

    private void AggregateMetrics(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            foreach (var series in _metrics.Values)
            {
                // Remove old metrics
                series.Values.RemoveAll(v => 
                    now - v.Timestamp > TimeSpan.FromDays(1));

                // Aggregate if needed
                if (series.Values.Count > 1000)
                {
                    var aggregated = series.Values
                        .GroupBy(v => v.Timestamp.Truncate(_aggregationInterval))
                        .Select(g => new MetricValue(
                            g.Average(v => v.Value),
                            g.Key))
                        .ToList();

                    series.Values = aggregated;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating metrics");
        }
    }

    private static MetricValue.MetricsSummary CalculateMetrics(MetricsSeries series, 
        DateTime startTime, DateTime endTime)
    {
        var values = series.Values
            .Where(v => v.Timestamp >= startTime && v.Timestamp <= endTime)
            .Select(v => v.Value)
            .ToList();

        if (!values.Any())
        {
            return new MetricValue.MetricsSummary();
        }

        return new MetricValue.MetricsSummary
        {
            Count = values.Count,
            Min = values.Min(),
            Max = values.Max(),
            Average = values.Average(),
            Median = CalculateMedian(values),
            Percentile95 = CalculatePercentile(values, 95),
            Percentile99 = CalculatePercentile(values, 99)
        };
    }

    private static double CalculateMedian(List<double> values)
    {
        var sortedValues = values.OrderBy(v => v).ToList();
        var mid = sortedValues.Count / 2;

        return sortedValues.Count % 2 == 0
            ? (sortedValues[mid - 1] + sortedValues[mid]) / 2
            : sortedValues[mid];
    }

    private static double CalculatePercentile(List<double> values, int percentile)
    {
        var sortedValues = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sortedValues.Count) - 1;
        return sortedValues[Math.Max(0, index)];
    }
}