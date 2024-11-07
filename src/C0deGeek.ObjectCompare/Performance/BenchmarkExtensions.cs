using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Performance;

/// <summary>
/// Extension methods for benchmark operations
/// </summary>
public static class BenchmarkExtensions
{
    public static async Task<BenchmarkResult> BenchmarkAsync<T>(
        this IComparer<T> comparer,
        string name,
        T obj1,
        T obj2,
        BenchmarkConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        var runner = new BenchmarkRunner(
            config ?? new BenchmarkConfig(),
            null);

        return await runner.RunBenchmarkAsync(
            name,
            async (x, y) =>
            {
                var result = new ComparisonResult
                {
                    AreEqual = comparer.Compare(x, y) == 0
                };
                return result;
            },
            obj1,
            obj2,
            cancellationToken);
    }
}