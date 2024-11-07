using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Tests.Models;

namespace C0deGeek.ObjectCompare.Tests;

[TestClass]
public class MetadataComparerTests
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
    public void Compare_WithIgnoredProperties_ExcludesCorrectly()
    {
        // Arrange
        _config.ExcludedProperties.Add("IgnoredProperty");
        var obj1 = new MetadataTestClass { Id = 1, Name = "Test", IgnoredProperty = "Different1" };
        var obj2 = new MetadataTestClass { Id = 1, Name = "Test", IgnoredProperty = "Different2" };

        // Act
        var result = _comparer.Compare(obj1, obj2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }
    
    [TestMethod]
    public void Compare_WithPrivateFields_HandlesCorrectly()
    {
        // Arrange
        _config.ComparePrivateFields = true;
        var obj1 = new MetadataTestClass("secret1") { Id = 1, Name = "Test" };
        var obj2 = new MetadataTestClass("secret2") { Id = 1, Name = "Test" };

        // Act
        var result = _comparer.Compare(obj1, obj2);

        // Assert - should detect difference in private field
        Assert.IsFalse(result.AreEqual);
        Assert.IsTrue(result.Differences.Any(d => d.Contains("Values differ: secret1 != secret2")));
    }
}