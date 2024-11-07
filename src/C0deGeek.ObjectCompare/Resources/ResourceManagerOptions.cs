using Microsoft.Extensions.Logging;

namespace C0deGeek.ObjectCompare.Resources;

/// <summary>
/// Configuration options for the ResourceManager
/// </summary>
public class ResourceManagerOptions
{
    public ILogger? Logger { get; set; }
    public int? MaxConcurrency { get; set; }
    public TimeSpan? DefaultTimeout { get; set; }
}