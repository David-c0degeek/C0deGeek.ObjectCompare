using C0deGeek.ObjectCompare.Common;
using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Cloning;

/// <summary>
/// Provides context and state for cloning operations
/// </summary>
public class CloneContext(ComparisonConfig config)
{
    private readonly Dictionary<object, object> _circularReferenceTracker = new(ReferenceEqualityComparer.Instance);
    
    private readonly ComparisonConfig _config = Guard.ThrowIfNull(config, nameof(config));

    public bool TryGetExistingClone(object original, out object? clone)
    {
        return _circularReferenceTracker.TryGetValue(original, out clone);
    }

    public void RegisterClone(object original, object clone)
    {
        _circularReferenceTracker[original] = clone;
    }

    public ComparisonConfig Config => _config;
}