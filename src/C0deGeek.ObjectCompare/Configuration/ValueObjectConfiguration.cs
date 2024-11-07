using C0deGeek.ObjectCompare.Enums;
using C0deGeek.ObjectCompare.Interfaces;

namespace C0deGeek.ObjectCompare.Configuration;

/// <summary>
/// Provides configuration options for value object comparison
/// </summary>
public class ValueObjectConfiguration
{
    public ComparisonMode ComparisonMode { get; set; } = ComparisonMode.Default;
    public bool UseRelativeComparison { get; set; } = true;
    public double DefaultTolerance { get; set; } = 1e-10;
    public Dictionary<Type, double> TypeTolerances { get; set; } = new();
    public Dictionary<Type, ICustomComparer> CustomComparers { get; set; } = new();
    public HashSet<string> ExcludedComponents { get; set; } = [];
    public bool EnableCaching { get; set; } = true;

    public ValueObjectOptions Options { get; set; } = new();

    public class ValueObjectOptions
    {
        public bool ComparePrivateComponents { get; set; }
        public bool IgnoreCase { get; set; }
        public bool IgnoreWhitespace { get; set; }
        public bool UseStrictComparison { get; set; }
        public NullHandling NullHandling { get; set; } = NullHandling.Strict;
        public int MaxRecursionDepth { get; set; } = 10;
    }

    public ValueObjectConfiguration Clone()
    {
        return new ValueObjectConfiguration
        {
            ComparisonMode = ComparisonMode,
            UseRelativeComparison = UseRelativeComparison,
            DefaultTolerance = DefaultTolerance,
            TypeTolerances = new Dictionary<Type, double>(TypeTolerances),
            CustomComparers = new Dictionary<Type, ICustomComparer>(CustomComparers),
            ExcludedComponents = [..ExcludedComponents],
            EnableCaching = EnableCaching,
            Options = new ValueObjectOptions
            {
                ComparePrivateComponents = Options.ComparePrivateComponents,
                IgnoreCase = Options.IgnoreCase,
                IgnoreWhitespace = Options.IgnoreWhitespace,
                UseStrictComparison = Options.UseStrictComparison,
                NullHandling = Options.NullHandling,
                MaxRecursionDepth = Options.MaxRecursionDepth
            }
        };
    }

    public double GetToleranceForType(Type type)
    {
        return TypeTolerances.GetValueOrDefault(type, DefaultTolerance);
    }

    public class Builder
    {
        private readonly ValueObjectConfiguration _config = new();

        public Builder WithComparisonMode(ComparisonMode mode)
        {
            _config.ComparisonMode = mode;
            return this;
        }

        public Builder WithDefaultTolerance(double tolerance)
        {
            _config.DefaultTolerance = tolerance;
            return this;
        }

        public Builder WithTypeTolerance<T>(double tolerance)
        {
            _config.TypeTolerances[typeof(T)] = tolerance;
            return this;
        }

        public Builder WithCustomComparer<T>(ICustomComparer comparer)
        {
            _config.CustomComparers[typeof(T)] = comparer;
            return this;
        }

        public Builder ExcludeComponent(string componentName)
        {
            _config.ExcludedComponents.Add(componentName);
            return this;
        }

        public Builder WithOptions(Action<ValueObjectOptions> configure)
        {
            configure(_config.Options);
            return this;
        }

        public ValueObjectConfiguration Build()
        {
            return _config.Clone();
        }
    }
}