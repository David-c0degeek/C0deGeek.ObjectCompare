# ObjectComparison

A high-performance, extensible .NET library for deep object comparison with support for complex types, collections, and dynamic objects.

## Features

- Deep comparison of objects with circular reference detection
- Object snapshot capability with intelligent cloning
- Support for custom comparison and cloning logic
- Collection comparison with order-sensitive and order-insensitive modes
- Dynamic object comparison support (ExpandoObject, DynamicObject)
- Floating-point comparison with configurable precision
- Advanced type handling:
  - Nullable types
  - Value types with safe default initialization
  - Reference types with proper null handling
  - Custom type cloning
- Thread-safe caching:
  - Type metadata caching
  - Expression compilation caching
  - Property access optimization
- Comprehensive comparison results with detailed differences
- Expression tree-based property access for performance
- Support for private field comparison
- Configurable maximum depth and object count limits

## Installation

```bash
dotnet add package ObjectComparison
```

## Quick Start

```csharp
// Create a comparer with default configuration
var comparer = new ObjectComparer();

// Compare two objects
var result = comparer.Compare(obj1, obj2);

if (!result.AreEqual)
{
    foreach (var difference in result.Differences)
    {
        Console.WriteLine(difference);
    }
}

// Take a snapshot for later comparison
var snapshot = comparer.TakeSnapshot(originalObject);
```

## Object Snapshots

The library provides powerful snapshot capabilities with intelligent cloning.

### Basic Snapshot Usage

```csharp
// Create an object and take a snapshot
var person = new Person
{
    Name = "John Doe",
    Age = 30,
    Hobbies = new List<string> { "Reading", "Gaming" }
};

var comparer = new ObjectComparer();
var snapshot = comparer.TakeSnapshot(person);

// Make changes to the original object
person.Age = 31;
person.Hobbies.Add("Cooking");

// Compare current state with snapshot
var result = comparer.Compare(person, snapshot);
```

### Custom Type Cloning

```csharp
var config = new ComparisonConfig
{
    CustomCloners = new Dictionary<Type, Func<object, object>>
    {
        { 
            typeof(DateTime), 
            obj => ((DateTime)obj).Date // Clone only the date part
        },
        {
            typeof(StringBuilder),
            obj => new StringBuilder(((StringBuilder)obj).ToString())
        }
    }
};

var comparer = new ObjectComparer(config);
```

### Type Handling

```csharp
// Value Types
struct Point { public int X, Y; }
var point = new Point { X = 1, Y = 2 };
var snapshot = comparer.TakeSnapshot(point); // Creates proper value copy

// Nullable Types
int? nullableValue = 42;
var snapshot = comparer.TakeSnapshot(nullableValue); // Preserves null state

// Collections
var list = new List<Point> { new() { X = 1, Y = 2 } };
var snapshot = comparer.TakeSnapshot(list); // Deep clones elements
```

## Advanced Configuration

```csharp
var config = new ComparisonConfig
{
    // Comparison Options
    ComparePrivateFields = true,
    DeepComparison = true,
    IgnoreCollectionOrder = true,
    
    // Precision Settings
    DecimalPrecision = 4,
    FloatingPointTolerance = 1e-10,
    UseRelativeFloatingPointComparison = true,
    
    // Null Handling
    NullValueHandling = NullHandling.Loose,
    
    // Performance Options
    MaxDepth = 10,
    MaxObjectCount = 10000,
    UseCachedMetadata = true,
    
    // Property Options
    ExcludedProperties = new HashSet<string> { "CachedValue", "LastModified" },
    CompareReadOnlyProperties = true,
    
    // Type-Specific Options
    CustomComparers = new Dictionary<Type, ICustomComparer>(),
    CollectionItemComparers = new Dictionary<Type, IEqualityComparer>(),
    
    // Diagnostics
    TrackPropertyPaths = true,
    Logger = loggerFactory.CreateLogger<ObjectComparer>()
};
```

## Error Handling

```csharp
try
{
    var result = comparer.Compare(obj1, obj2);
}
catch (ComparisonException ex) when (ex.Path != null)
{
    Console.WriteLine($"Comparison failed at path: {ex.Path}");
    Console.WriteLine($"Error: {ex.Message}");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid argument: {ex.Message}");
}
```

### Common Exceptions

- `ComparisonException`: General comparison failures
- `ArgumentException`: Invalid types or configurations
- `InvalidOperationException`: Operation not supported for type
- `NotSupportedException`: Unsupported type or operation

## Performance Optimization

### Caching

```csharp
// Enable all caching features
var config = new ComparisonConfig
{
    UseCachedMetadata = true,
    TrackPropertyPaths = false // Disable for better performance
};

// Pre-compile frequently used types
TypeCache.GetMetadata(typeof(MyFrequentType), true);
```

### Memory Management

```csharp
// Limit object graph traversal
var config = new ComparisonConfig
{
    MaxDepth = 5,
    MaxObjectCount = 1000,
    ExcludedProperties = new HashSet<string> { "LargeCollection", "CachedData" }
};
```

### Collection Handling

```csharp
// Fast collection comparison for simple types
var config = new ComparisonConfig
{
    IgnoreCollectionOrder = true, // Uses optimized comparison
    CollectionItemComparers = new Dictionary<Type, IEqualityComparer>
    {
        { typeof(int), EqualityComparer<int>.Default }
    }
};
```

## Thread Safety

The library uses several thread-safe mechanisms:
- Concurrent collections for caches
- Immutable configuration objects
- Thread-local contexts for comparison state
- Lock-free algorithms where possible

### Safe Concurrent Usage

```csharp
// Create once and reuse
private static readonly ObjectComparer SharedComparer = new();

// Safe for concurrent use
public async Task CompareAsync(object obj1, object obj2)
{
    await Task.Run(() => SharedComparer.Compare(obj1, obj2));
}
```

## Best Practices

1. Reuse `ObjectComparer` instances
2. Configure appropriate depth limits
3. Use custom comparers for complex types
4. Enable caching for repeated comparisons
5. Handle circular references appropriately
6. Use type-specific equality comparers
7. Consider memory usage with large object graphs
8. Implement proper error handling
9. Use logging for troubleshooting
10. Test with representative data sets

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)  
This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.MD) file for details.