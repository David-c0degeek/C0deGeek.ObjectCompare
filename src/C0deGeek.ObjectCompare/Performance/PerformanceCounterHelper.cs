using System.Diagnostics;

namespace C0deGeek.ObjectCompare.Performance;

internal class PerformanceCounterHelper
{
    private readonly Process _process;
    private DateTime _lastTime;
    private TimeSpan _lastTotalProcessorTime;

    public PerformanceCounterHelper()
    {
        _process = Process.GetCurrentProcess();
        _lastTime = DateTime.UtcNow;
        _lastTotalProcessorTime = _process.TotalProcessorTime;
    }

    public double GetCpuUsage()
    {
        var currentTime = DateTime.UtcNow;
        var currentTotalProcessorTime = _process.TotalProcessorTime;

        var timeDiff = currentTime - _lastTime;
        var processorTimeDiff = currentTotalProcessorTime - _lastTotalProcessorTime;

        _lastTime = currentTime;
        _lastTotalProcessorTime = currentTotalProcessorTime;

        return processorTimeDiff.TotalSeconds / 
            (timeDiff.TotalSeconds * Environment.ProcessorCount) * 100;
    }
}