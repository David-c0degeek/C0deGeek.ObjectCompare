using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Enums;

namespace C0deGeek.ObjectCompare.Tests;

[TestClass]
public class ComparisonConfigurationTests
{
    [TestMethod]
    public void Config_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new ComparisonConfig();

        // Assert
        Assert.IsTrue(config.DeepComparison);
        Assert.AreEqual(100, config.MaxDepth);
        Assert.AreEqual(10000, config.MaxObjectCount);
        Assert.AreEqual(TimeSpan.FromMinutes(5), config.ComparisonTimeout);
        Assert.AreEqual(NullHandling.Strict, config.NullValueHandling);
    }

    [TestMethod]
    public void Config_Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new ComparisonConfig
        {
            MaxDepth = 5,
            ExcludedProperties = ["Test"]
        };

        // Act
        var clone = original.Clone();
        original.MaxDepth = 10;
        original.ExcludedProperties.Add("Another");

        // Assert
        Assert.AreEqual(5, clone.MaxDepth);
        Assert.AreEqual(1, clone.ExcludedProperties.Count);
        Assert.IsTrue(clone.ExcludedProperties.Contains("Test"));
    }
}