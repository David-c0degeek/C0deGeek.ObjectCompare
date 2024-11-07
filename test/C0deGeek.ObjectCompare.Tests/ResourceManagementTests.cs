using C0deGeek.ObjectCompare.Comparison.Base;

namespace C0deGeek.ObjectCompare.Tests;

[TestClass]
public class ResourceManagementTests
{
    private ObjectComparer _comparer = null!;
    private ComparisonConfig _config = null!;

    [TestInitialize]
    public void Setup()
    {
        _config = new ComparisonConfig();
        _comparer = new ObjectComparer(_config);
    }

    [TestMethod]
    public void Compare_MultipleCalls_ManaagesResourcesEfficiently()
    {
        // Arrange
        const int iterations = 1000;
        var initialMemory = GC.GetTotalMemory(true);
        var obj1 = new { Id = 1, Name = "Test" };
        var obj2 = new { Id = 1, Name = "Test" };

        // Act
        for (var i = 0; i < iterations; i++)
        {
            _comparer.Compare(obj1, obj2);
        }

        GC.Collect();
        var finalMemory = GC.GetTotalMemory(true);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        Assert.IsTrue(memoryIncrease < 1024 * 1024, // Less than 1MB increase
            $"Memory usage increased by {memoryIncrease / 1024.0:F2}KB");
    }

    [TestMethod]
    public void Dispose_ReleasesResources()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        using (var disposableComparer = new ObjectComparer(_config))
        {
            var obj1 = new { Id = 1, Data = new byte[1024] }; // Reduce size
            var obj2 = new { Id = 1, Data = new byte[1024] };
            disposableComparer.Compare(obj1, obj2);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect(); // Second collection to ensure cleanup
        var finalMemory = GC.GetTotalMemory(true);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        Assert.IsTrue(memoryIncrease < 1024 * 100); // Allow for some overhead
    }
}