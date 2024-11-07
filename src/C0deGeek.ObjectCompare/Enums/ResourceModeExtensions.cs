namespace C0deGeek.ObjectCompare.Enums;

/// <summary>
/// Provides extension methods for resource mode
/// </summary>
public static class ResourceModeExtensions
{
    public static bool IsAutomaticMode(this ResourceMode mode) =>
        mode == ResourceMode.Automatic;

    public static bool IsManualMode(this ResourceMode mode) =>
        mode == ResourceMode.Manual;

    public static bool IsPooledMode(this ResourceMode mode) =>
        mode == ResourceMode.Pooled;

    public static bool RequiresExplicitCleanup(this ResourceMode mode) =>
        mode == ResourceMode.Manual || mode == ResourceMode.Aggressive;

    public static bool SupportsPooling(this ResourceMode mode) =>
        mode == ResourceMode.Pooled ||
        mode == ResourceMode.PerformanceOptimized;

    public static bool IsOptimizedMode(this ResourceMode mode) =>
        mode == ResourceMode.MemoryOptimized ||
        mode == ResourceMode.PerformanceOptimized;
}