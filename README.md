# C0deGeek.ObjectCompare

A high-performance, feature-rich .NET library for deep object comparison with advanced capabilities for complex types, collections, and dynamic objects. This library provides precise control over comparison behavior, extensive configuration options, and robust performance optimizations.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue)

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Features](#core-features)
  - [Basic Comparison](#basic-comparison)
  - [Deep Comparison](#deep-comparison)
  - [Collection Comparison](#collection-comparison)
  - [Value Type Comparison](#value-type-comparison)
  - [Dynamic Object Comparison](#dynamic-object-comparison)
- [Advanced Features](#advanced-features)
  - [Snapshots](#snapshots)
  - [Custom Comparers](#custom-comparers)
  - [Floating-Point Comparison](#floating-point-comparison)
  - [Circular References](#circular-references)
  - [Path Tracking](#path-tracking)
- [Performance Features](#performance-features)
  - [Caching](#caching)
  - [Parallel Processing](#parallel-processing)
  - [Resource Management](#resource-management)
  - [Benchmarking](#benchmarking)
- [Configuration](#configuration)
  - [Basic Configuration](#basic-configuration)
  - [Advanced Configuration](#advanced-configuration)
  - [Performance Configuration](#performance-configuration)
- [Extension Methods](#extension-methods)
- [Best Practices](#best-practices)
- [Examples](#examples)
- [Performance Optimization](#performance-optimization)
- [Thread Safety](#thread-safety)
- [Error Handling](#error-handling)
- [Contributing](#contributing)
- [License](#license)

## Features

- Deep comparison of objects with circular reference detection
- Configurable comparison strategies for different types
- High-performance implementation using expression trees and caching
- Support for:
  - Value types with precision control
  - Reference types with proper null handling
  - Collections (ordered and unordered)
  - Dynamic objects (ExpandoObject, DynamicObject)
  - Custom comparison logic
- Advanced features:
  - Object snapshots with intelligent cloning
  - Detailed difference tracking
  - Path tracking
  - Metric collection
  - Async comparison
- Performance optimizations:
  - Expression compilation caching
  - Type metadata caching
  - Property access optimization
  - Parallel processing
  - Resource pooling
- Comprehensive configuration options
- Thread-safe operations
- Extensive error handling

## Installation

```bash
dotnet add package C0deGeek.ObjectCompare
```

## Quick Start

```csharp
using C0deGeek.ObjectCompare;
using C0deGeek.ObjectCompare.Comparison.Base;

// Basic comparison
var comparer = new ObjectComparer();
var result = comparer.Compare(obj1, obj2);

if (!result.AreEqual)
{
    foreach (var difference in result.Differences)
    {
        Console.WriteLine(difference);
    }
}

// Using extension methods
if (obj1.DeepEquals(obj2))
{
    Console.WriteLine("Objects are equal!");
}

// Taking snapshots
var snapshot = comparer.TakeSnapshot(originalObject);
```

## Core Features

### Basic Comparison

```csharp
// Simple comparison
var comparer = new ObjectComparer();
var result = comparer.Compare(obj1, obj2);

// With configuration
var config = new ComparisonConfig
{
    ComparePrivateFields = true,
    IgnoreCollectionOrder = true
};

var configuredComparer = new ObjectComparer(config);
var detailedResult = configuredComparer.Compare(obj1, obj2);

// Using extension methods
bool areEqual = obj1.DeepEquals(obj2);
```

### Deep Comparison

```csharp
var config = new ComparisonConfig
{
    DeepComparison = true,
    MaxDepth = 10,
    ContinueOnDifference = true
};

var comparer = new ObjectComparer(config);
var result = comparer.Compare(complexObj1, complexObj2);

// Get detailed differences
foreach (var diff in result.Differences)
{
    Console.WriteLine($"Difference found: {diff}");
}
```

### Collection Comparison

```csharp
// Ordered comparison
var orderedConfig = new ComparisonConfig
{
    IgnoreCollectionOrder = false
};

// Unordered comparison
var unorderedConfig = new ComparisonConfig
{
    IgnoreCollectionOrder = true
};

// Custom collection item comparison
var config = new ComparisonConfig();
config.CollectionItemComparers[typeof(MyClass)] = new MyClassEqualityComparer();

// Compare collections
var comparer = new ObjectComparer(config);
var result = comparer.Compare(list1, list2);
```

### Value Type Comparison

```csharp
// Floating-point comparison
var config = new ComparisonConfig
{
    FloatingPointTolerance = 1e-10,
    UseRelativeFloatingPointComparison = true
};

// Decimal comparison
config.DecimalPrecision = 4;

// Custom value comparison
public class DateOnlyComparer : ICustomComparer
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

config.CustomComparers[typeof(DateTime)] = new DateOnlyComparer();
```

### Dynamic Object Comparison

```csharp
// Compare ExpandoObjects
dynamic obj1 = new ExpandoObject();
obj1.Name = "Test";
obj1.Value = 42;

dynamic obj2 = new ExpandoObject();
obj2.Name = "Test";
obj2.Value = 42;

var comparer = new ObjectComparer();
var result = comparer.Compare(obj1, obj2);

// Compare custom dynamic objects
public class DynamicPerson : DynamicObject
{
    private readonly Dictionary<string, object> _properties = new();

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        return _properties.TryGetValue(binder.Name, out result);
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        _properties[binder.Name] = value;
        return true;
    }
}

var person1 = new DynamicPerson();
person1.Name = "John";
var person2 = new DynamicPerson();
person2.Name = "John";

var result = comparer.Compare(person1, person2);
```

## Advanced Features

### Snapshots

```csharp
// Take a snapshot
var snapshot = comparer.TakeSnapshot(originalObject);

// Modify the original
originalObject.SomeProperty = "New Value";

// Compare with snapshot
var result = comparer.Compare(originalObject, snapshot);

// Async snapshot
var asyncComparer = new AsyncObjectComparer();
var asyncSnapshot = await asyncComparer.TakeSnapshotAsync(originalObject);
```

### Custom Comparers

```csharp
public class CustomPersonComparer : ICustomComparer
{
    public bool AreEqual(object obj1, object obj2, ComparisonConfig config)
    {
        if (obj1 is Person p1 && obj2 is Person p2)
        {
            return p1.Id == p2.Id && 
                   p1.Name.Equals(p2.Name, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}

var config = new ComparisonConfig();
config.CustomComparers[typeof(Person)] = new CustomPersonComparer();

// Using builder pattern
var config = new ComparisonConfig.Builder()
    .WithCustomComparer<Person>(new CustomPersonComparer())
    .WithMaxDepth(5)
    .Build();
```

### Floating-Point Comparison

```csharp
var config = new ComparisonConfig
{
    FloatingPointTolerance = 1e-10,
    UseRelativeFloatingPointComparison = true
};

// Using specific comparer
var floatComparer = new FloatingPointComparer(tolerance: 1e-10);

// Using ULP-based comparison
var ulpComparer = FloatingPointComparer.CreateUlpComparer(maxUlps: 4);

// Extension method usage
if (value1.AreFloatingPointEqual(value2, config))
{
    Console.WriteLine("Values are equal within tolerance");
}
```

### Circular References

```csharp
public class Node
{
    public string Value { get; set; }
    public Node Next { get; set; }
}

var node1 = new Node { Value = "A" };
node1.Next = node1; // Self-reference

var node2 = new Node { Value = "A" };
node2.Next = node2;

var comparer = new ObjectComparer();
var result = comparer.Compare(node1, node2); // Handles circular reference
```

### Path Tracking

```csharp
var config = new ComparisonConfig
{
    TrackPropertyPaths = true
};

var comparer = new ObjectComparer(config);
var result = comparer.Compare(obj1, obj2);

foreach (var path in result.DifferentPaths)
{
    Console.WriteLine($"Difference at: {path}");
}

// Get detailed differences with values
var details = obj1.GetDetailedDifferences(obj2);
foreach (var (path, value1, value2) in details)
{
    Console.WriteLine($"Path: {path}");
    Console.WriteLine($"Value1: {value1}");
    Console.WriteLine($"Value2: {value2}");
}
```

## Performance Features

### Caching

```csharp
// Enable caching
var config = new ComparisonConfig
{
    UseCachedMetadata = true,
    Caching = new CachingOptions
    {
        EnableCaching = true,
        CacheTimeout = TimeSpan.FromHours(1),
        MaxCacheSize = 10000,
        EnableMetadataCaching = true,
        EnableExpressionCaching = true
    }
};

// Pre-compile frequently used types
TypeCache.GetMetadata(typeof(FrequentType), true);
```

### Parallel Processing

```csharp
var config = new ComparisonConfig();
config.Performance.EnableParallelProcessing = true;
config.Performance.MaxDegreeOfParallelism = Environment.ProcessorCount;
config.Performance.BatchSize = 100;

var asyncComparer = new AsyncObjectComparer(config);
var result = await asyncComparer.CompareAsync(obj1, obj2);
```

### Resource Management

```csharp
using var resourceManager = new ResourceManager();

// Register resources
resourceManager.RegisterResource("myComparer", new ObjectComparer());

// Use resource pooling
using (var scope = await resourceManager.AcquireResourceScopeAsync<ObjectComparer>())
{
    var result = scope.Resource.Compare(obj1, obj2);
}

// Configure resource management
var config = new ComparisonConfig();
config.Performance.ResourceManagement.MaxMemoryUsage = 1024 * 1024 * 1024; // 1GB
config.Performance.ResourceManagement.EnableResourcePooling = true;
config.Performance.ResourceManagement.PoolSize = 10;
```

### Benchmarking

```csharp
var benchmarkConfig = new BenchmarkConfig
{
    Iterations = 100,
    WarmupIterations = 5,
    CollectGarbage = true,
    TrackMemory = true,
    DetailedMetrics = true
};

var runner = new BenchmarkRunner(benchmarkConfig);
var result = await runner.RunBenchmarkAsync(
    "MyComparison",
    async (x, y) => await comparer.CompareAsync(x, y),
    obj1,
    obj2
);

Console.WriteLine($"Average time: {result.AverageTime}");
Console.WriteLine($"Memory used: {result.MemoryUsed}");
Console.WriteLine($"95th percentile: {result.Percentile95}");
```

## Configuration

### Basic Configuration

```csharp
var config = new ComparisonConfig
{
    ComparePrivateFields = true,
    DeepComparison = true,
    MaxDepth = 10,
    IgnoreCollectionOrder = true,
    NullValueHandling = NullHandling.Strict,
    DecimalPrecision = 4
};
```

### Advanced Configuration

```csharp
var config = new ComparisonConfig.Builder()
    .WithMaxDepth(10)
    .WithTimeout(TimeSpan.FromMinutes(5))
    .WithCustomComparer<DateTime>(new DateOnlyComparer())
    .WithCollectionComparer<Person>(new PersonEqualityComparer())
    .WithNullHandling(NullHandling.Loose)
    .WithComparisonOptions(opt =>
    {
        opt.ComparePrivateFields = true;
        opt.IgnoreCase = true;
        opt.FloatingPointTolerance = 1e-10;
    })
    .Build();
```

### Performance Configuration

```csharp
var config = new PerformanceConfiguration.Builder()
    .WithParallelProcessing()
    .WithMaxDegreeOfParallelism(Environment.ProcessorCount)
    .WithBatchSize(100)
    .WithResourceManagement(rm =>
    {
        rm.MaxMemoryUsage = 1024 * 1024 * 1024;
        rm.EnableResourcePooling = true;
        rm.PoolSize = 10;
    })
    .WithThreading(t =>
    {
        t.UseThreadPool = true;
        t.MinThreads = 4;
        t.MaxThreads = 16;
        t.EnableWorkStealing = true;
    })
    .WithMetrics(m =>
    {
        m.TrackDetailedMetrics = true;
        m.EnableHistograms = true;
    })
    .Build();
```

## Extension Methods

```csharp
// Simple comparison
if (obj1.DeepEquals(obj2))
{
    Console.WriteLine("Objects are equal");
}

// Async comparison
if (await obj1.DeepEqualsAsync(obj2))
{
    Console.WriteLine("Objects are equal");
}

// Get differences
var differences = obj1.GetDifferences(obj2);

// Compare with configuration
var result = obj1.CompareWith(obj2, config =>
{
    config.IgnoreCollectionOrder = true;
    config.ComparePrivateFields = true;
});

// Async comparison with configuration
var result = await obj1.CompareWithAsync(obj2, config =>
{
    config.MaxDepth = 5;
    config.DeepComparison = true;
});

// Structure comparison
if (obj1.HasSameStructure(obj2))
{
    Console.WriteLine("Objects have same structure");
}
```

## Best Practices

1. **Reuse Comparers**
   ```csharp
   // DO: Create once and reuse
   private static readonly ObjectComparer SharedComparer = new();
   
   // DON'T: Create new instance for each comparison
   var result = new ObjectComparer().Compare(obj1, obj2);
   ```

2. **Configure Appropriate Depth**
   ```csharp
   var config = new ComparisonConfig
   {
       MaxDepth = 10, // Set based on your object graph
       ```csharp
       MaxObjectCount = 1000 // Prevent runaway comparisons
   };
   ```

3. **Handle Collections Efficiently**
   ```csharp
   var config = new ComparisonConfig
   {
       // Use unordered comparison for better performance when order doesn't matter
       IgnoreCollectionOrder = true,
       
       // Configure batch size for large collections
       Performance = new PerformanceOptions
       {
           BatchSize = 100,
           EnableParallelProcessing = true
       }
   };
   ```

4. **Optimize Memory Usage**
   ```csharp
   var config = new ComparisonConfig
   {
       // Clear caches periodically
       Caching = new CachingOptions
       {
           MaxCacheSize = 1000,
           CacheTimeout = TimeSpan.FromHours(1)
       },
       
       // Exclude large properties
       ExcludedProperties = new HashSet<string> 
       { 
           "LargeCollection", 
           "BinaryData" 
       }
   };
   ```

5. **Implement Custom Comparers for Complex Types**
   ```csharp
   public class MoneyComparer : ICustomComparer
   {
       public bool AreEqual(object obj1, object obj2, ComparisonConfig config)
       {
           if (obj1 is Money m1 && obj2 is Money m2)
           {
               return m1.Amount == m2.Amount && 
                      m1.Currency.Equals(m2.Currency);
           }
           return false;
       }
   }

   config.CustomComparers[typeof(Money)] = new MoneyComparer();
   ```

## Examples

### Complex Object Comparison

```csharp
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Birthday { get; set; }
    public List<Address> Addresses { get; set; }
    public Dictionary<string, string> Attributes { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
}

// Configure comparison
var config = new ComparisonConfig.Builder()
    .WithMaxDepth(5)
    .WithComparisonOptions(opt =>
    {
        opt.IgnoreCase = true;
        opt.CompareReadOnlyProperties = false;
    })
    .WithCustomComparer<DateTime>(new DateOnlyComparer())
    .Build();

var comparer = new ObjectComparer(config);

// Compare objects
var person1 = new Person
{
    Id = 1,
    Name = "John Doe",
    Birthday = new DateTime(1990, 1, 1),
    Addresses = new List<Address>
    {
        new() { Street = "123 Main St", City = "Boston", Country = "USA" }
    },
    Attributes = new Dictionary<string, string>
    {
        { "Title", "Mr." },
        { "Language", "English" }
    }
};

var person2 = // ... similar person object

var result = comparer.Compare(person1, person2);

if (!result.AreEqual)
{
    foreach (var difference in result.Differences)
    {
        Console.WriteLine(difference);
    }
}
```

### Asynchronous Comparison with Progress

```csharp
public class ProgressHandler : IProgress<ComparisonProgress>
{
    public void Report(ComparisonProgress value)
    {
        Console.WriteLine($"Progress: {value.PercentageComplete}%");
        Console.WriteLine($"Items Processed: {value.ProcessedItems}/{value.TotalItems}");
        Console.WriteLine($"Differences Found: {value.Differences}");
        Console.WriteLine($"Time Elapsed: {value.ElapsedTime}");
    }
}

var progress = new ProgressHandler();
var cts = new CancellationTokenSource();

try
{
    var asyncComparer = new AsyncObjectComparer();
    var result = await asyncComparer.CompareAsync(
        largeObject1,
        largeObject2,
        progress,
        cts.Token
    );

    Console.WriteLine($"Comparison completed in {result.ComparisonTime}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Comparison was cancelled");
}
```

### Value Object Comparison

```csharp
public class Coordinate : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }

    public Coordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }

    public override bool EqualsWithTolerance(ValueObject other, double tolerance)
    {
        if (other is not Coordinate coord)
            return false;

        return Math.Abs(Latitude - coord.Latitude) <= tolerance &&
               Math.Abs(Longitude - coord.Longitude) <= tolerance;
    }
}

var config = new ComparisonConfig
{
    FloatingPointTolerance = 0.0001
};

var comparer = new ObjectComparer(config);
var coord1 = new Coordinate(51.5074, -0.1278);
var coord2 = new Coordinate(51.5075, -0.1279);

var result = comparer.Compare(coord1, coord2);
```

### Collection Comparison with Custom Matching

```csharp
public class OrderComparer : IMatchingCollectionComparer
{
    public bool CompareWithMatching(
        IEnumerable collection1,
        IEnumerable collection2,
        string path,
        ComparisonResult result,
        Func<object, object, bool> matchingPredicate)
    {
        var orders1 = collection1.Cast<Order>().ToList();
        var orders2 = collection2.Cast<Order>().ToList();

        if (orders1.Count != orders2.Count)
        {
            result.AddDifference($"Order count mismatch: {orders1.Count} vs {orders2.Count}", path);
            return false;
        }

        var matched = new bool[orders2.Count];
        var isEqual = true;

        for (var i = 0; i < orders1.Count; i++)
        {
            var matchFound = false;
            for (var j = 0; j < orders2.Count; j++)
            {
                if (matched[j]) continue;
                
                if (matchingPredicate(orders1[i], orders2[j]))
                {
                    matched[j] = true;
                    matchFound = true;
                    break;
                }
            }

            if (!matchFound)
            {
                result.AddDifference($"No matching order found for OrderId: {orders1[i].OrderId}", path);
                isEqual = false;
            }
        }

        return isEqual;
    }
}

var config = new ComparisonConfig();
config.CollectionItemComparers[typeof(Order)] = new OrderComparer();

var comparer = new ObjectComparer(config);
var result = comparer.Compare(orderList1, orderList2);
```

## Performance Optimization

### Memory Management

```csharp
var config = new ComparisonConfig
{
    // Configure caching
    Caching = new CachingOptions
    {
        EnableCaching = true,
        MaxCacheSize = 1000,
        CacheTimeout = TimeSpan.FromMinutes(30)
    },

    // Configure resource management
    Performance = new PerformanceOptions
    {
        ResourceManagement = new ResourceManagementOptions
        {
            MaxMemoryUsage = 512 * 1024 * 1024, // 512MB
            EnableResourcePooling = true,
            PoolSize = 10
        }
    }
};

// Monitor performance
var monitor = new PerformanceMonitor(logger);

using (monitor.TrackOperation("ComplexComparison"))
{
    var result = comparer.Compare(obj1, obj2);
}

var report = monitor.GenerateReport();
Console.WriteLine($"Memory Used: {report.MemoryUsage.WorkingSet}");
Console.WriteLine($"CPU Usage: {report.CpuUsage}%");
```

### Thread Safety

```csharp
// Thread-safe comparison
public class ComparisonService
{
    private static readonly ObjectComparer SharedComparer = new();
    private readonly AsyncObjectComparer _asyncComparer;
    private readonly SemaphoreSlim _throttle;

    public ComparisonService(int maxConcurrency = 10)
    {
        _asyncComparer = new AsyncObjectComparer();
        _throttle = new SemaphoreSlim(maxConcurrency);
    }

    public async Task<ComparisonResult> CompareAsync<T>(T obj1, T obj2)
    {
        await _throttle.WaitAsync();
        try
        {
            return await _asyncComparer.CompareAsync(obj1, obj2);
        }
        finally
        {
            _throttle.Release();
        }
    }

    public ComparisonResult Compare<T>(T obj1, T obj2)
    {
        // SharedComparer is thread-safe
        return SharedComparer.Compare(obj1, obj2);
    }
}
```

## Error Handling

```csharp
try
{
    var result = comparer.Compare(obj1, obj2);
}
catch (MaximumDepthExceededException ex)
{
    Console.WriteLine($"Max depth {ex.MaxDepth} exceeded at {ex.Path}");
}
catch (MaximumObjectCountExceededException ex)
{
    Console.WriteLine($"Max object count {ex.MaxObjectCount} exceeded");
}
catch (CircularReferenceException ex)
{
    Console.WriteLine($"Circular reference detected at {ex.Path}");
    Console.WriteLine($"Property path: {string.Join(" -> ", ex.PropertyPath)}");
}
catch (ComparisonException ex)
{
    Console.WriteLine($"Comparison failed: {ex.Message}");
    Console.WriteLine($"Path: {ex.Path}");
    foreach (var (key, value) in ex.Context)
    {
        Console.WriteLine($"{key}: {value}");
    }
}
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Please ensure your PR:
- Follows existing code style
- Includes appropriate tests
- Updates documentation
- Does not break existing tests

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.MD) file for details.