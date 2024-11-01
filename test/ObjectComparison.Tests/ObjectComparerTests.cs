using C0deGeek.ObjectCompare;

namespace ObjectComparison.Tests;

[TestClass]
public class ObjectComparerTests
{
    private ObjectComparer _comparer = null!;
    private ComparisonConfig _config = null!;

    [TestInitialize]
    public void Setup()
    {
        _config = new ComparisonConfig();
        _comparer = new ObjectComparer(_config);
    }

    #region Basic Type Tests

    [TestMethod]
    public void Compare_SimpleTypes_Equal()
    {
        // Arrange
        int value1 = 42;
        int value2 = 42;

        // Act
        var result = _comparer.Compare(value1, value2);

        // Assert
        Assert.IsTrue(result.AreEqual);
        Assert.AreEqual(0, result.Differences.Count);
    }

    [TestMethod]
    public void Compare_SimpleTypes_NotEqual()
    {
        // Arrange
        int value1 = 42;
        int value2 = 43;

        // Act
        var result = _comparer.Compare(value1, value2);

        // Assert
        Assert.IsFalse(result.AreEqual);
        Assert.AreEqual(1, result.Differences.Count);
    }

    [TestMethod]
    public void Compare_NullableTypes_BothNull()
    {
        // Arrange
        int? value1 = null;
        int? value2 = null;

        // Act
        var result = _comparer.Compare(value1, value2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    [TestMethod]
    public void Compare_NullableTypes_OneNull()
    {
        // Arrange
        int? value1 = 42;
        int? value2 = null;

        // Act
        var result = _comparer.Compare(value1, value2);

        // Assert
        Assert.IsFalse(result.AreEqual);
        Assert.IsTrue(result.Differences[0].Contains("null"));
    }

    #endregion

    #region Complex Type Tests

    [TestMethod]
    public void Compare_ComplexTypes_Equal()
    {
        // Arrange
        var obj1 = new TestClass { Id = 1, Name = "Test" };
        var obj2 = new TestClass { Id = 1, Name = "Test" };

        // Act
        var result = _comparer.Compare(obj1, obj2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    [TestMethod]
    public void Compare_ComplexTypes_DifferentProperty()
    {
        // Arrange
        var obj1 = new TestClass { Id = 1, Name = "Test1" };
        var obj2 = new TestClass { Id = 1, Name = "Test2" };

        // Act
        var result = _comparer.Compare(obj1, obj2);

        // Assert
        Assert.IsFalse(result.AreEqual);
        Assert.IsTrue(result.Differences[0].Contains("Name"));
    }

    [TestMethod]
    public void Compare_CircularReference_Handled()
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

    #region Collection Tests

    [TestMethod]
    public void Compare_Lists_Equal()
    {
        // Arrange
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 1, 2, 3 };

        // Act
        var result = _comparer.Compare(list1, list2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    [TestMethod]
    public void Compare_Lists_DifferentOrder_WithIgnoreOrder()
    {
        // Arrange
        _config.IgnoreCollectionOrder = true;
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 3, 2, 1 };

        // Act
        var result = _comparer.Compare(list1, list2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    [TestMethod]
    public void Compare_Lists_DifferentOrder_WithoutIgnoreOrder()
    {
        // Arrange
        _config.IgnoreCollectionOrder = false;
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 3, 2, 1 };

        // Act
        var result = _comparer.Compare(list1, list2);

        // Assert
        Assert.IsFalse(result.AreEqual);
    }

    [TestMethod]
    public void Compare_Arrays_Equal()
    {
        // Arrange
        var array1 = new[] { 1, 2, 3 };
        var array2 = new[] { 1, 2, 3 };

        // Act
        var result = _comparer.Compare(array1, array2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    #endregion

    #region Custom Comparison Tests

    [TestMethod]
    public void Compare_WithCustomComparer_Used()
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

    [TestMethod]
    public void Compare_WithCustomComparer_DifferentDates()
    {
        // Arrange
        _config.CustomComparers[typeof(DateTime)] = new DateOnlyComparer();
        var date1 = new DateTime(2024, 1, 1);
        var date2 = new DateTime(2024, 1, 2);

        // Act
        var result = _comparer.Compare(date1, date2);

        // Assert
        Assert.IsFalse(result.AreEqual);
    }

    #endregion

    #region Snapshot Tests

    [TestMethod]
    public void TakeSnapshot_SimpleType_CreatesIndependentCopy()
    {
        // Arrange
        var original = new TestClass { Id = 1, Name = "Test" };

        // Act
        var snapshot = _comparer.TakeSnapshot(original);
        original.Name = "Modified";

        // Assert
        Assert.AreEqual("Test", snapshot.Name);
    }

    [TestMethod]
    public void TakeSnapshot_Collection_DeepClones()
    {
        // Arrange
        var original = new List<TestClass>
        {
            new() { Id = 1, Name = "Test" }
        };

        // Act
        var snapshot = _comparer.TakeSnapshot(original);
        original[0].Name = "Modified";

        // Assert
        Assert.AreEqual("Test", snapshot[0].Name);
    }

    [TestMethod]
    public void TakeSnapshot_CircularReference_HandledCorrectly()
    {
        // Arrange
        var original = new CircularReferenceClass { Id = 1 };
        original.Reference = original; // Self-reference

        // Act
        var snapshot = _comparer.TakeSnapshot(original);

        // Assert
        Assert.AreEqual(original.Id, snapshot.Id); // Values should match
        Assert.IsNotNull(snapshot.Reference); // Reference should exist
        Assert.AreSame(snapshot, snapshot.Reference); // Should point to itself, not create new instance
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Compare_MaxDepthReached_ReportsInResult()
    {
        // Arrange
        _config.MaxDepth = 1;
        var obj1 = new NestedClass { 
            Inner = new NestedClass { 
                Value = "Deep",  // Add a value to ensure difference
                Inner = new NestedClass() 
            } 
        };
        var obj2 = new NestedClass { 
            Inner = new NestedClass { 
                Value = "Different",  // Different value
                Inner = new NestedClass() 
            } 
        };

        // Act
        var result = _comparer.Compare(obj1, obj2);

        // Assert
        Assert.IsFalse(result.AreEqual);
        Assert.IsTrue(result.MaxDepthReached > 0);
        Assert.IsNotNull(result.MaxDepthPath);
    }

    [TestMethod]
    public void Compare_MaxObjectCountReached_ReportsInResult()
    {
        // Arrange
        _config.MaxObjectCount = 2;
        var obj1 = new List<string> { "1", "2", "3" };
        var obj2 = new List<string> { "1", "2", "3" };

        // Act
        var result = _comparer.Compare(obj1, obj2);

        // Assert
        Assert.IsFalse(result.AreEqual);
        Assert.IsTrue(result.Differences[0].Contains("maximum object count"));
    }

    #endregion

    #region Test Classes

    private class TestClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private class CircularReferenceClass
    {
        public int Id { get; set; }
        public CircularReferenceClass? Reference { get; set; }
    }

    private class NestedClass
    {
        public NestedClass? Inner { get; set; }
        public string? Value { get; set; }
    }

    private class DateOnlyComparer : ICustomComparer
    {
        public bool AreEqual(object obj1, object obj2, ComparisonConfig config)
        {
            if (obj1 is DateTime date1 && obj2 is DateTime date2)
            {
                return date1.Date == date2.Date;
            }
            return false;
        }
    }

    #endregion
}