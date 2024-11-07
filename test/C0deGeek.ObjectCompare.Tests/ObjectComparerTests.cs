using System.Dynamic;
using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Comparison.Exceptions;
using C0deGeek.ObjectCompare.Enums;
using C0deGeek.ObjectCompare.Tests.Comparers;
using C0deGeek.ObjectCompare.Tests.Extensions;
using C0deGeek.ObjectCompare.Tests.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace C0deGeek.ObjectCompare.Tests;

[TestClass]
public class ObjectComparerTests
{
    private ObjectComparer _comparer = null!;
    private ComparisonConfig _config = null!;
    private ILogger _logger = null!;

    [TestInitialize]
    public void Setup()
    {
        _logger = new Mock<ILogger>().Object;
        _config = new ComparisonConfig
        {
            Logger = _logger,
            MaxDepth = 10,
            MaxObjectCount = 1000,
            ComparisonTimeout = TimeSpan.FromSeconds(5),
            DeepComparison = true
        };
        _comparer = new ObjectComparer(_config);
    }

    #region Simple Value Comparisons

    [TestMethod]
    [DataRow(1, 1, true)]
    [DataRow(1, 2, false)]
    [DataRow(int.MaxValue, int.MaxValue, true)]
    [DataRow(int.MinValue, int.MinValue, true)]
    public void Compare_SimpleIntegers_ReturnsExpectedResult(int value1, int value2, bool expectedEqual)
    {
        // Act
        var result = _comparer.Compare(value1, value2);

        // Assert
        Assert.AreEqual(expectedEqual, result.AreEqual);
    }

    [TestMethod]
    [DataRow("test", "test", true)]
    [DataRow("test", "Test", false)]
    [DataRow("", "", true)]
    [DataRow(null, null, true)]
    [DataRow("test", null, false)]
    public void Compare_Strings_ReturnsExpectedResult(string? value1, string? value2, bool expectedEqual)
    {
        // Act
        var result = _comparer.Compare(value1, value2);

        // Assert
        Assert.AreEqual(expectedEqual, result.AreEqual);
    }

    [TestMethod]
    public void Compare_FloatingPointValues_HandlesEpsilonCorrectly()
    {
        // Arrange
        _config.FloatingPointTolerance = 1e-10;
        _config.UseRelativeFloatingPointComparison = true;
        var value1 = 0.1 + 0.2;
        var value2 = 0.3;

        // Act
        var result = _comparer.Compare(value1, value2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    [TestMethod]
    public void Compare_Decimals_HandlesPrecisionCorrectly()
    {
        // Arrange
        _config.DecimalPrecision = 2;
        decimal value1 = 1.234m;
        decimal value2 = 1.239m;

        // Act
        var result = _comparer.Compare(value1, value2);

        // Assert
        Assert.IsFalse(result.AreEqual);
    }

    #endregion

    #region Null Handling Tests

    [TestMethod]
    public void Compare_NullValues_StrictMode()
    {
        // Arrange
        _config.NullValueHandling = NullHandling.Strict;
        string? value1 = null;
        string value2 = string.Empty;

        // Act
        var result = _comparer.Compare(value1, value2);

        // Assert
        Assert.IsFalse(result.AreEqual);
    }

    [TestMethod]
    public void Compare_NullValues_LooseMode()
    {
        // Arrange
        _config.NullValueHandling = NullHandling.Loose;
        string? value1 = null;
        string value2 = string.Empty;

        // Act
        var result = _comparer.Compare(value1, value2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    #endregion

    #region Complex Object Tests

    [TestMethod]
    public void Compare_ComplexObjects_Equal()
    {
        // Arrange
        var obj1 = new TestClass { Id = 1, Name = "Test", Values = [1, 2, 3] };
        var obj2 = new TestClass { Id = 1, Name = "Test", Values = [1, 2, 3] };

        // Act
        var result = _comparer.Compare(obj1, obj2);

        // Assert
        Assert.IsTrue(result.AreEqual);
        Assert.AreEqual(0, result.Differences.Count);
    }

    [TestMethod]
    public void Compare_ComplexObjects_Different()
    {
        // Arrange
        var obj1 = new TestClass 
        { 
            Id = 1, 
            Name = "Test1", 
            Values = [1, 2, 3]
        };
        var obj2 = new TestClass 
        { 
            Id = 1, 
            Name = "Test2", 
            Values = [1, 2, 4]
        };

        // Act
        var result = _comparer.Compare(obj1, obj2);
    
        // Assert
        Assert.IsFalse(result.AreEqual);
        Assert.IsTrue(result.Differences.Any(), "Should have differences");
    }

    #endregion

    #region Collection Tests

    [TestMethod]
    public void Compare_Collections_OrderSensitive()
    {
        // Arrange
        _config.IgnoreCollectionOrder = false;
        var list1 = new[] { 1, 2, 3 };
        var list2 = new[] { 3, 2, 1 };

        // Act
        var result = _comparer.Compare(list1, list2);

        // Assert
        Assert.IsFalse(result.AreEqual);
    }

    [TestMethod]
    public void Compare_Collections_OrderInsensitive()
    {
        // Arrange
        _config.IgnoreCollectionOrder = true;
        var list1 = new[] { 1, 2, 3 };
        var list2 = new[] { 3, 2, 1 };

        // Act
        var result = _comparer.Compare(list1, list2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    [TestMethod]
    public void Compare_NestedCollections_HandlesCorrectly()
    {
        // Arrange
        var list1 = new[] { [1, 2], new[] { 3, 4 } };
        var list2 = new[] { [1, 2], new[] { 3, 4 } };

        // Act
        var result = _comparer.Compare(list1, list2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    #endregion

    #region Custom Comparison Tests

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
    }

    #endregion

    #region Circular Reference Tests

    [TestMethod]
    public void Compare_CircularReference_HandlesCorrectly()
    {
        // Arrange
        var obj1 = new CircularReferenceClass { Id = 1 };
        var obj2 = new CircularReferenceClass { Id = 1 };
        obj1.Reference = obj1;
        obj2.Reference = obj2;

        // Act
        var result = _comparer.Compare(obj1, obj2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    #endregion

    #region Dynamic Object Tests

    [TestMethod]
    public void Compare_DynamicObjects_Equal()
    {
        // Arrange
        dynamic obj1 = new ExpandoObject();
        obj1.Name = "Test";
        obj1.Value = 42;

        dynamic obj2 = new ExpandoObject();
        obj2.Name = "Test";
        obj2.Value = 42;

        // Act
        var result = _comparer.Compare((object)obj1, (object)obj2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    [TestMethod]
    public void Compare_DynamicObjects_Different()
    {
        // Arrange
        dynamic obj1 = new ExpandoObject();
        obj1.Name = "Test1";

        dynamic obj2 = new ExpandoObject();
        obj2.Name = "Test2";

        // Act
        var result = _comparer.Compare((object)obj1, (object)obj2);

        // Assert
        Assert.IsFalse(result.AreEqual);
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    [ExpectedException(typeof(MaximumObjectCountExceededException))]
    public void Compare_ExceedsMaxObjectCount_ThrowsException()
    {
        // Arrange
        _config.MaxObjectCount = 5; // Maximum number of objects to compare
        var obj1 = new ComplexObject { Id = 1 };
        var obj2 = new ComplexObject { Id = 1 };

        // Each SimpleObject counts as an object, plus the ComplexObject itself
        // So with MaxObjectCount = 5, adding 6 items should exceed the limit
        for (var i = 0; i < 6; i++)
        {
            obj1.Items.Add(new SimpleObject { Id = i });
            obj2.Items.Add(new SimpleObject { Id = i });
        }

        // Act - should throw MaximumObjectCountExceededException
        _comparer.Compare(obj1, obj2);
    }
    
    [TestMethod]
    [ExpectedException(typeof(ComparisonException))]
    public void Compare_MaxDepthReached_ReportsInResult()
    {
        // Arrange
        _config.MaxDepth = 1;
        var obj1 = TestObjectHelper.CreateNestedObject(3); // Create object deeper than max
        var obj2 = TestObjectHelper.CreateNestedObject(3);

        var result = _comparer.Compare(obj1, obj2);
    }

    #endregion

    #region Snapshot Tests

    [TestMethod]
    public void TakeSnapshot_CreatesIndependentCopy()
    {
        // Arrange
        var original = new TestClass 
        { 
            Id = 1, 
            Name = "Test", 
            Values = new[] { 1, 2, 3 } 
        };

        // Act
        var snapshot = _comparer.TakeSnapshot(original);
        Assert.IsNotNull(snapshot);
    
        // Save snapshot state before modifications
        var snapshotValuesBefore = snapshot.Values.ToArray();

        // Modify original in different ways
        original.Id = 99;
        original.Name = "Modified";
        original.Values = new[] { 4, 5, 6 }; // Change array reference
    
        // Assert
        Assert.AreEqual(1, snapshot.Id, "Id should not change");
        Assert.AreEqual("Test", snapshot.Name, "Name should not change");
        CollectionAssert.AreEqual(
            new[] { 1, 2, 3 }, 
            snapshot.Values, 
            "Array values should not change");
    }

    [TestMethod]
    public void TakeSnapshot_HandlesCircularReferences()
    {
        // Arrange
        var original = new CircularReferenceClass { Id = 1 };
        original.Reference = original;

        // Act
        var snapshot = _comparer.TakeSnapshot(original);

        // Assert
        Assert.AreEqual(original.Id, snapshot.Id);
        Assert.IsNotNull(snapshot.Reference);
        Assert.AreSame(snapshot, snapshot.Reference);
    }

    #endregion
}