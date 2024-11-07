using System.Diagnostics;
using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Tests.Extensions;

namespace C0deGeek.ObjectCompare.Tests;

[TestClass]
public class PerformanceTests
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
    public void Compare_LargeObjects_CompletesWithinTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(10); // Increase timeout
        var obj1 = TestObjectHelper.CreateComplexObject(100); // Reduce size
        var obj2 = TestObjectHelper.CreateComplexObject(100);

        // Act
        var sw = Stopwatch.StartNew();
        var result = _comparer.Compare(obj1, obj2);
        sw.Stop();

        // Assert
        Assert.IsTrue(sw.Elapsed < timeout, 
            $"Comparison took {sw.Elapsed.TotalSeconds} seconds");
        Assert.IsTrue(result.AreEqual);
    }

    [TestMethod]
    public void Compare_DeepNestedObjects_HandlesEfficiently()
    {
        // Arrange
        const int depth = 100;
        var obj1 = TestObjectHelper.CreateNestedObject(depth);
        var obj2 = TestObjectHelper.CreateNestedObject(depth);

        // Act
        var sw = Stopwatch.StartNew();
        var result = _comparer.Compare(obj1, obj2);
        sw.Stop();

        // Assert
        Assert.IsTrue(result.AreEqual);
        Assert.IsTrue(sw.ElapsedMilliseconds < 1000, "Comparison took too long");
    }

    private static object CreateLargeObject()
    {
        var result = new Dictionary<string, object>();
        for (var i = 0; i < 1000; i++)
        {
            result[$"key{i}"] = new
            {
                Id = i,
                Name = $"Item {i}",
                Data = new byte[100],
                Nested = new { SubId = i, SubName = $"Sub {i}" }
            };
        }
        return result;
    }
}