using System.Diagnostics;
using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Tests.Extensions;
using C0deGeek.ObjectCompare.Tests.Models;

namespace C0deGeek.ObjectCompare.Tests;

[TestClass]
public class ComprehensiveObjectComparerTests
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
    [DataRow(1, 1, true)]
    [DataRow(1, 2, false)]
    [DataRow(null, null, true)]
    [DataRow(1, null, false)]
    public void Compare_PrimitiveTypes_ReturnsExpectedResult(int? value1, int? value2, bool expectedEqual)
    {
        // Act
        var result = _comparer.Compare(value1, value2);

        // Assert
        Assert.AreEqual(expectedEqual, result.AreEqual);
    }

    [TestMethod]
    public void Compare_ComplexObjectWithCircularReference_HandlesCorrectly()
    {
        // Arrange
        var obj1 = new CircularObject { Id = 1 };
        obj1.Reference = obj1;
        
        var obj2 = new CircularObject { Id = 1 };
        obj2.Reference = obj2;

        // Act
        var result = _comparer.Compare(obj1, obj2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    /*
    [TestMethod]
    public void Compare_WithCancellation_CancelsOperation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var complexObj1 = CreateLargeObject();
        var complexObj2 = CreateLargeObject();

        // Act & Assert
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        Assert.ThrowsException<OperationCanceledException>(() =>
            _comparer.Compare(complexObj1, complexObj2, cts.Token));
    }

    [TestMethod]
    public void Compare_WithCustomComparer_UsesCustomLogic()
    {
        // Arrange
        _config.CustomComparers[typeof(DateTime)] = new DateOnlyComparer();
        var date1 = new DateTime(2024, 1, 1, 10, 0, 0);
        var date2 = new DateTime(2024, 1, 1, 15, 0, 0);

        // Act
        var result = _comparer.Compare(date1, date2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }*/

    [TestMethod]
    public void Compare_FloatingPointValues_HandlesEpsilonCorrectly()
    {
        // Arrange
        var value1 = 0.1 + 0.2;
        var value2 = 0.3;

        // Act
        var result = _comparer.Compare(value1, value2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    [TestMethod]
    public void Compare_LargeObjects_CompletesWithinTimeout()
    {
        // Arrange
        var obj1 = TestObjectHelper.CreateLargeObject();
        var obj2 = TestObjectHelper.CreateLargeObject();
        var timeout = TimeSpan.FromSeconds(5);

        // Act
        var sw = Stopwatch.StartNew();
        var result = _comparer.Compare(obj1, obj2);
        sw.Stop();

        // Assert
        Assert.IsTrue(sw.Elapsed < timeout, 
            $"Comparison took {sw.Elapsed.TotalSeconds} seconds, exceeding timeout of {timeout.TotalSeconds} seconds");
    }

    [TestMethod]
    public void Compare_Collections_HandlesDifferentOrderings()
    {
        // Arrange
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 3, 1, 2 };

        // Test ordered comparison
        _config.IgnoreCollectionOrder = false;
        var orderedResult = _comparer.Compare(list1, list2);
        Assert.IsFalse(orderedResult.AreEqual);

        // Test unordered comparison
        _config.IgnoreCollectionOrder = true;
        var unorderedResult = _comparer.Compare(list1, list2);
        Assert.IsTrue(unorderedResult.AreEqual);
    }
}