# ObjectComparison

A high-performance, extensible .NET library for deep object comparison with support for complex types, collections, and dynamic objects.

## Features

- Deep comparison of objects with circular reference detection
- Object snapshot capability for state tracking and comparison
- Support for custom comparison logic
- Collection comparison with order-sensitive and order-insensitive modes
- Dynamic object comparison support (ExpandoObject, DynamicObject)
- Floating-point comparison with configurable precision
- Nullable type handling
- Thread-safe caching of type metadata
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

The library provides powerful snapshot capabilities for tracking object state changes over time.

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

// Check differences
if (!result.AreEqual)
{
    foreach (var difference in result.Differences)
    {
        Console.WriteLine(difference);
    }
}
```

### Collection Snapshots

```csharp
var comparer = new ObjectComparer(new ComparisonConfig
{
    IgnoreCollectionOrder = true // Optional: ignore item order
});

var people = new List<Person>
{
    new() { Name = "Alice", Age = 25 },
    new() { Name = "Bob", Age = 30 }
};

// Take snapshot of the collection
var snapshot = comparer.TakeSnapshot(people);

// Modify collection
people[0].Age = 26;
people.Add(new Person { Name = "Charlie", Age = 35 });

// Compare with snapshot
var result = comparer.Compare(people, snapshot);
```

### Periodic Snapshots

```csharp
var comparer = new ObjectComparer();
var snapshots = new Dictionary<DateTime, Person>();

var person = new Person { Name = "Jane", Age = 28 };

// Take periodic snapshots
snapshots[DateTime.Now] = comparer.TakeSnapshot(person);

person.Age = 29;
snapshots[DateTime.Now.AddMonths(1)] = comparer.TakeSnapshot(person);

// Compare changes between any two points in time
var result = comparer.Compare(
    snapshots.First().Value, 
    snapshots.Last().Value
);
```

### Snapshot Features

- Deep cloning of object graphs
- Support for circular references
- Thread-safe operation
- Works with all supported types:
    - Simple objects
    - Complex object graphs
    - Collections
    - Dynamic objects
- Configurable comparison options
- No serialization required
- Maintains object integrity

## Advanced Configuration

```csharp
var config = new ComparisonConfig
{
    ComparePrivateFields = true,
    DeepComparison = true,
    DecimalPrecision = 4,
    IgnoreCollectionOrder = true,
    MaxDepth = 10,
    NullValueHandling = NullHandling.Loose,
    FloatingPointTolerance = 1e-10,
    UseRelativeFloatingPointComparison = true
};

var comparer = new ObjectComparer(config);
```

## Custom Comparison Logic

```csharp
public class CustomDateComparer : ICustomComparer
{
    public bool AreEqual(object obj1, object obj2, ComparisonConfig config)
    {
        if (obj1 is DateTime date1 && obj2 is DateTime date2)
        {
            return date1.Date == date2.Date; // Compare only dates, ignore time
        }
        return false;
    }
}

var config = new ComparisonConfig
{
    CustomComparers = new Dictionary<Type, ICustomComparer>
    {
        { typeof(DateTime), new CustomDateComparer() }
    }
};
```

## Collection Comparison

```csharp
// Order-sensitive comparison
var config = new ComparisonConfig { IgnoreCollectionOrder = false };
var comparer = new ObjectComparer(config);
var result = comparer.Compare(list1, list2);

// Order-insensitive comparison
config.IgnoreCollectionOrder = true;
result = comparer.Compare(list1, list2);
```

## Dynamic Object Support

```csharp
dynamic obj1 = new ExpandoObject();
obj1.Name = "Test";
obj1.Value = 42;

dynamic obj2 = new ExpandoObject();
obj2.Name = "Test";
obj2.Value = 42;

var result = comparer.Compare(obj1, obj2);
```

## Performance Considerations

- Use `UseCachedMetadata = true` for better performance with repeated comparisons
- Set appropriate `MaxDepth` and `MaxObjectCount` limits for large object graphs
- Consider using custom comparers for complex type comparisons
- Use `ComparePrivateFields = false` when only public property comparison is needed
- Snapshots are stored in memory, so manage them appropriately for large objects

## Thread Safety

The library is thread-safe and can be used in concurrent scenarios. Type metadata, compiled expressions, and snapshots are handled in a thread-safe manner.

## Error Handling

```csharp
try
{
    var result = comparer.Compare(obj1, obj2);
}
catch (ComparisonException ex)
{
    Console.WriteLine($"Comparison failed at path: {ex.Path}");
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Logging

```csharp
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});

var config = new ComparisonConfig
{
    Logger = loggerFactory.CreateLogger<ObjectComparer>()
};
```

## License

MIT License

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.