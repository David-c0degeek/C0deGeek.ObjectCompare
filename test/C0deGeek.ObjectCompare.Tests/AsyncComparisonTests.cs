using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Tests.Models;

namespace C0deGeek.ObjectCompare.Tests;

[TestClass]
public class AsyncComparisonTests
{
    private AsyncObjectComparer _asyncComparer = null!;
    private ComparisonConfig _config = null!;

    [TestInitialize]
    public void Setup()
    {
        _config = new ComparisonConfig();
        _asyncComparer = new AsyncObjectComparer(_config);
    }

    [TestMethod]
    public async Task CompareAsync_SimpleValues_ReturnsExpectedResult()
    {
        // Arrange
        var value1 = 42;
        var value2 = 42;

        // Act
        var result = await _asyncComparer.CompareAsync(value1, value2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    [TestMethod]
    public async Task CompareAsync_LargeCollections_CompletesSuccessfully()
    {
        // Arrange
        var list1 = Enumerable.Range(1, 10000).ToList();
        var list2 = Enumerable.Range(1, 10000).ToList();

        // Act
        var result = await _asyncComparer.CompareAsync(list1, list2);

        // Assert
        Assert.IsTrue(result.AreEqual);
    }

    [TestMethod]
    public async Task CompareAsync_WithCancellation_StopsOperation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var list1 = Enumerable.Range(1, 1000000).Select(i => new ComplexObject { Id = i }).ToList();
        var list2 = Enumerable.Range(1, 1000000).Select(i => new ComplexObject { Id = i }).ToList();

        // Act & Assert
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() =>
            _asyncComparer.CompareAsync(list1, list2, cts.Token));
    }

    /*[TestMethod]
    public async Task CompareAsync_Timeout_ThrowsException()
    {
        // Arrange
        _config.ComparisonTimeout = TimeSpan.FromMilliseconds(1);
    
        var obj1 = new ComplexObject();
        var obj2 = new ComplexObject();

        // Add lots of items to make comparison time-consuming
        for (var i = 0; i < 100000; i++) // Increased from 10000
        {
            obj1.Items.Add(new SimpleObject 
            { 
                Id = i,
                Name = new string('x', 10000), // Increased from 1000
                Data = new byte[10000] // Increased from 1000
            });
            obj2.Items.Add(new SimpleObject
            {
                Id = i,
                Name = new string('x', 10000),
                Data = new byte[10000]
            });
        }

        // Act & Assert
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() =>
            _asyncComparer.CompareAsync(obj1, obj2));
    }*/
}