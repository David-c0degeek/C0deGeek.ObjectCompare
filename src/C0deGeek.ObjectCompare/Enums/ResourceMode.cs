namespace C0deGeek.ObjectCompare.Enums;

/// <summary>
/// Defines the mode of resource management
/// </summary>
public enum ResourceMode
{
    /// <summary>
    /// Automatic resource management
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Manual resource management
    /// </summary>
    Manual = 1,

    /// <summary>
    /// Pooled resource management
    /// </summary>
    Pooled = 2,

    /// <summary>
    /// Lazy resource management
    /// </summary>
    Lazy = 3,

    /// <summary>
    /// Aggressive resource management with immediate cleanup
    /// </summary>
    Aggressive = 4,

    /// <summary>
    /// Conservative resource management with delayed cleanup
    /// </summary>
    Conservative = 5,

    /// <summary>
    /// Resource management with memory optimization
    /// </summary>
    MemoryOptimized = 6,

    /// <summary>
    /// Resource management with performance optimization
    /// </summary>
    PerformanceOptimized = 7,

    /// <summary>
    /// Custom resource management strategy
    /// </summary>
    Custom = 8
}