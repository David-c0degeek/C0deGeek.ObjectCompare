using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Enums;

/// <summary>
/// Provides extension methods for comparison flags
/// </summary>
public static class ComparisonFlagsExtensions
{
    public static bool HasFlag(this ComparisonFlags flags, ComparisonFlags flag) =>
        (flags & flag) == flag;

    public static ComparisonFlags SetFlag(this ComparisonFlags flags, 
        ComparisonFlags flag, bool value = true) =>
        value ? flags | flag : flags & ~flag;

    public static ComparisonFlags CombineFlags(this ComparisonFlags flags, 
        params ComparisonFlags[] additionalFlags) =>
        additionalFlags.Aggregate(flags, (current, flag) => current | flag);

    public static ComparisonConfig ToConfig(this ComparisonFlags flags) =>
        new()
        {
            ComparePrivateFields = flags.HasFlag(ComparisonFlags.IncludePrivateMembers),
            CompareReadOnlyProperties = flags.HasFlag(
                ComparisonFlags.IncludeReadOnlyProperties),
            DeepComparison = flags.HasFlag(ComparisonFlags.DeepComparison),
            IgnoreCollectionOrder = flags.HasFlag(ComparisonFlags.IgnoreCollectionOrder),
            UseCachedMetadata = flags.HasFlag(ComparisonFlags.EnableCaching),
            TrackPropertyPaths = flags.HasFlag(ComparisonFlags.TrackPaths)
        };
}