using System.Collections.Concurrent;
using System.Diagnostics;
using C0deGeek.ObjectCompare.Common;
using Microsoft.Extensions.Logging;

namespace C0deGeek.ObjectCompare.Performance;

/// <summary>
/// Monitors and tracks performance metrics for comparison operations
/// </summary>
public class PerformanceMonitor(ILogger logger)
{
    private readonly ILogger _logger = Guard.ThrowIfNull(logger, nameof(logger));
    private readonly ConcurrentDictionary<string, Stopwatch> _operations = new();
    private readonly ConcurrentDictionary<string, long> _objectCounts = new();
    private readonly ConcurrentDictionary<string, List<TimeSpan>> _operationTimes = new();
    private readonly PerformanceCounterHelper _counterHelper = new();

    public IDisposable TrackOperation(string operationName)
    {
        var stopwatch = new Stopwatch();
        _operations[operationName] = stopwatch;
        stopwatch.Start();
        
        return new OperationTracker(this, operationName);
    }

    public void IncrementObjectCount(string type)
    {
        _objectCounts.AddOrUpdate(type, 1, (_, count) => count + 1);
    }

    public void TrackOperationTime(string operation, TimeSpan duration)
    {
        _operationTimes.AddOrUpdate(operation,
            [duration],
            (_, times) =>
            {
                times.Add(duration);
                return times;
            });
    }

    public PerformanceReport GenerateReport()
    {
        return new PerformanceReport
        {
            OperationTimes = _operations.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Elapsed),
            ObjectCounts = _objectCounts.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value),
            AverageOperationTimes = _operationTimes.ToDictionary(
                kvp => kvp.Key,
                kvp => new TimeSpan((long)kvp.Value.Average(t => t.Ticks))),
            MemoryUsage = GetMemoryUsage(),
            CpuUsage = _counterHelper.GetCpuUsage()
        };
    }

    private static MemoryMetrics GetMemoryUsage()
    {
        var process = Process.GetCurrentProcess();
        return new MemoryMetrics
        {
            WorkingSet = process.WorkingSet64,
            PrivateMemory = process.PrivateMemorySize64,
            ManagedMemory = GC.GetTotalMemory(false),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        };
    }

    private class OperationTracker(PerformanceMonitor monitor, string operationName) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;

            if (monitor._operations.TryGetValue(operationName, out var stopwatch))
            {
                stopwatch.Stop();
                monitor.TrackOperationTime(operationName, stopwatch.Elapsed);
                monitor._logger.LogDebug(
                    "Operation {Operation} completed in {Duration}ms",
                    operationName, stopwatch.ElapsedMilliseconds);
            }

            _disposed = true;
        }
    }
}