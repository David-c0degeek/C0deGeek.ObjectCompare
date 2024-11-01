﻿using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace C0deGeek.ObjectCompare
{
    /// <summary>
    /// Main object comparer class with optimized implementation
    /// </summary>
    public class ObjectComparer
    {
        private readonly ComparisonConfig _config;
        private readonly ExpressionCloner _cloner;

        public ObjectComparer(ComparisonConfig? config = null)
        {
            _config = config ?? new ComparisonConfig();
            _cloner = new ExpressionCloner(_config);
        }

        public T? TakeSnapshot<T>(T? obj)
        {
            return _cloner.Clone(obj);
        }

        public ComparisonResult Compare<T>(T? obj1, T? obj2)
        {
            var context = new ComparisonContext();
            var result = new ComparisonResult();

            try
            {
                context.Timer.Start();
                CompareObjectsIterative(obj1, obj2, "", result, context);
            }
            catch (Exception ex)
            {
                throw new ComparisonException("Comparison failed", "", ex);
            }
            finally
            {
                context.Timer.Stop();
                result.ComparisonTime = context.Timer.Elapsed;
                result.ObjectsCompared = context.ObjectsCompared;
                result.MaxDepthReached = context.MaxDepthReached;
            }

            return result;
        }

        private void CompareObjectsIterative(object? obj1, object? obj2, string path,
            ComparisonResult result, ComparisonContext context)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(context);

            var stack = new Stack<(object? Obj1, object? Obj2, string Path, int Depth)>();
            stack.Push((obj1, obj2, path, 0));

            while (stack.Count > 0 && context.ObjectsCompared < _config.MaxObjectCount)
            {
                var (current1, current2, currentPath, depth) = stack.Pop();
                if (current1 != null)
                {
                    context.PushObject(current1);
                }

                try
                {
                    if (depth >= _config.MaxDepth)
                    {
                        result.MaxDepthPath = currentPath;
                        result.MaxDepthReached = depth;
                        result.AreEqual = false;
                        result.Differences.Add($"Maximum depth of {_config.MaxDepth} reached at {currentPath}");
                        continue;
                    }

                    if (HandleNulls(current1, current2, currentPath, result))
                    {
                        continue;
                    }

                    var type = current1?.GetType() ?? current2?.GetType();
                    if (type == null) continue;

                    var metadata = TypeCache.GetMetadata(type, _config.UseCachedMetadata);

                    // Handle circular references
                    var pair = new ComparisonContext.ComparisonPair(
                        current1 ?? throw new InvalidOperationException("Unexpected null value"),
                        current2 ?? throw new InvalidOperationException("Unexpected null value"));

                    if (!context.ComparedObjects.Add(pair))
                    {
                        continue;
                    }

                    if (_config.CustomComparers.TryGetValue(type, out var customComparer))
                    {
                        HandleCustomComparison(customComparer, current1, current2, currentPath, result);
                        continue;
                    }

                    if (metadata.UnderlyingType != null)
                    {
                        CompareNullableTypes(current1, current2, currentPath, result, metadata);
                    }
                    else if (metadata.IsSimpleType)
                    {
                        CompareSimpleTypes(current1, current2, currentPath, result, metadata);
                    }
                    else if (metadata.IsCollection)
                    {
                        CompareCollections(current1, current2, currentPath, result, stack, depth, metadata);
                    }
                    else
                    {
                        CompareComplexObjects(current1, current2, currentPath, result, metadata, stack, depth);
                    }
                }
                finally
                {
                    if (current1 != null)
                    {
                        context.PopObject();
                    }
                }
            }

            if (context.ObjectsCompared >= _config.MaxObjectCount)
            {
                result.Differences.Add(
                    $"Comparison aborted: exceeded maximum object count of {_config.MaxObjectCount}");
                result.AreEqual = false;
            }
        }

        private bool HandleNulls(object? obj1, object? obj2, string path, ComparisonResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            if (ReferenceEquals(obj1, obj2)) return true;

            if (obj1 is null || obj2 is null)
            {
                if (_config.NullValueHandling == NullHandling.Loose && IsEmpty(obj1) && IsEmpty(obj2))
                {
                    return true;
                }

                result.Differences.Add($"Null difference at {path}: one object is null while the other is not");
                result.AreEqual = false;
                return true;
            }

            return false;
        }

        private void HandleCustomComparison(ICustomComparer comparer, object? obj1, object? obj2,
            string path, ComparisonResult result)
        {
            ArgumentNullException.ThrowIfNull(comparer);
            ArgumentNullException.ThrowIfNull(result);

            try
            {
                if (obj1 == null || obj2 == null)
                {
                    result.Differences.Add($"Custom comparison failed at {path}: one or both objects are null");
                    result.AreEqual = false;
                    return;
                }

                if (!comparer.AreEqual(obj1, obj2, _config))
                {
                    result.Differences.Add($"Custom comparison failed at {path}");
                    result.AreEqual = false;
                }
            }
            catch (Exception ex)
            {
                throw new ComparisonException($"Custom comparison failed at {path}", path, ex);
            }
        }

        private void CompareNullableTypes(object? obj1, object? obj2, string path,
            ComparisonResult result, TypeMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(metadata);

            try
            {
                var value1 = obj1 != null ? TypeCache.GetPropertyGetter(obj1.GetType(), "Value")(obj1) : null;
                var value2 = obj2 != null ? TypeCache.GetPropertyGetter(obj2.GetType(), "Value")(obj2) : null;

                if (metadata.UnderlyingType == typeof(decimal))
                {
                    CompareDecimals(value1 as decimal?, value2 as decimal?, path, result);
                    return;
                }

                if (metadata is { HasCustomEquality: true, EqualityComparer: not null })
                {
                    // Skip comparison if both values are null
                    if (value1 == null && value2 == null) return;

                    // Handle cases where one value is null
                    if (value1 == null || value2 == null)
                    {
                        result.Differences.Add($"Nullable value difference at {path}: {value1} != {value2}");
                        result.AreEqual = false;
                        return;
                    }

                    // Safe to call EqualityComparer as we've verified neither value is null
                    if (metadata.EqualityComparer(value1, value2)) return;
                    
                    result.Differences.Add($"Nullable value difference at {path}: {value1} != {value2}");
                    result.AreEqual = false;
                    return;
                }

                if (Equals(value1, value2)) return;
                
                result.Differences.Add($"Nullable value difference at {path}: {value1} != {value2}");
                result.AreEqual = false;
            }
            catch (Exception ex)
            {
                throw new ComparisonException($"Failed to compare nullable values at {path}", path, ex);
            }
        }

        private void CompareDecimals(decimal? dec1, decimal? dec2, string path, ComparisonResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            var value1 = dec1 ?? 0m;
            var value2 = dec2 ?? 0m;
            var rounded1 = Math.Round(value1, _config.DecimalPrecision);
            var rounded2 = Math.Round(value2, _config.DecimalPrecision);

            if (rounded1 == rounded2) return;
            
            result.Differences.Add($"Decimal difference at {path}: {rounded1} != {rounded2}");
            result.AreEqual = false;
        }

        private void CompareComplexObjects(object? obj1, object? obj2, string path,
            ComparisonResult result, TypeMetadata metadata,
            Stack<(object? Obj1, object? Obj2, string Path, int Depth)> stack, int depth)
        {
            if (obj1 == null || obj2 == null)
            {
                result.Differences.Add($"Null object in complex comparison at {path}");
                result.AreEqual = false;
                return;
            }

            // Compare properties
            foreach (var prop in metadata.Properties)
            {
                if (!ShouldCompareProperty(prop)) continue;

                try
                {
                    var getter = TypeCache.GetPropertyGetter(obj1.GetType(), prop.Name);
                    var value1 = getter(obj1);
                    var value2 = getter(obj2);

                    if (_config.DeepComparison)
                    {
                        stack.Push((value1, value2, $"{path}.{prop.Name}", depth + 1));
                    }
                    else if (!AreValuesEqual(value1, value2))
                    {
                        result.Differences.Add($"Property difference at {path}.{prop.Name}");
                        result.AreEqual = false;
                    }
                }
                catch (Exception ex)
                {
                    _config.Logger?.LogWarning(ex, "Failed to compare property {Property} at {Path}", prop.Name, path);
                    throw new ComparisonException($"Error comparing property {prop.Name}", path, ex);
                }
            }

            if (_config.ComparePrivateFields)
            {
                CompareFields(obj1, obj2, path, result, metadata, stack, depth);
            }
        }

        private void CompareFields(object obj1, object obj2, string path,
            ComparisonResult result, TypeMetadata metadata,
            Stack<(object? Obj1, object? Obj2, string Path, int Depth)> stack, int depth)
        {
            foreach (var field in metadata.Fields)
            {
                if (_config.ExcludedProperties.Contains(field.Name)) continue;

                try
                {
                    var value1 = field.GetValue(obj1);
                    var value2 = field.GetValue(obj2);

                    if (_config.DeepComparison)
                    {
                        stack.Push((value1, value2, $"{path}.{field.Name}", depth + 1));
                    }
                    else if (!AreValuesEqual(value1, value2))
                    {
                        result.Differences.Add($"Field difference at {path}.{field.Name}");
                        result.AreEqual = false;
                    }
                }
                catch (Exception ex)
                {
                    _config.Logger?.LogWarning(ex, "Failed to compare field {Field} at {Path}", field.Name, path);
                    throw new ComparisonException($"Error comparing field {field.Name}", path, ex);
                }
            }
        }

        private static bool AreValuesEqual(object? value1, object? value2)
        {
            if (ReferenceEquals(value1, value2)) return true;
            if (value1 == null || value2 == null) return false;

            try
            {
                return value1.Equals(value2);
            }
            catch (Exception)
            {
                // If Equals throws an exception, try reverse comparison
                try
                {
                    return value2.Equals(value1);
                }
                catch (Exception)
                {
                    // If both comparisons fail, consider values not equal
                    return false;
                }
            }
        }

        private static bool IsEmpty(object? obj)
        {
            return obj switch
            {
                null => true,
                string str => string.IsNullOrEmpty(str),
                IEnumerable enumerable => !enumerable.Cast<object>().Any(),
                _ => false
            };
        }

        private sealed class FastEqualityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object? x, object? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null || y == null) return false;
                return x.Equals(y);
            }

            public int GetHashCode(object? obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }

        private void CompareSimpleTypes(object obj1, object obj2, string path,
            ComparisonResult result, TypeMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(obj1);
            ArgumentNullException.ThrowIfNull(obj2);
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(metadata);

            if (metadata is { HasCustomEquality: true, EqualityComparer: not null })
            {
                try
                {
                    if (metadata.EqualityComparer(obj1, obj2)) return;
                    
                    result.Differences.Add($"Value difference at {path}: {obj1} != {obj2}");
                    result.AreEqual = false;

                    return;
                }
                catch (Exception ex)
                {
                    throw new ComparisonException($"Custom equality comparison failed at {path}", path, ex);
                }
            }

            if (obj1 is decimal dec1 && obj2 is decimal dec2)
            {
                CompareDecimals(dec1, dec2, path, result);
                return;
            }

            if (obj1 is float f1 && obj2 is float f2)
            {
                if (NumericComparison.AreFloatingPointEqual(f1, f2, _config)) return;
                
                result.Differences.Add($"Float difference at {path}: {f1} != {f2}");
                result.AreEqual = false;

                return;
            }

            if (obj1 is double d1 && obj2 is double d2)
            {
                if (NumericComparison.AreFloatingPointEqual(d1, d2, _config)) return;
                
                result.Differences.Add($"Double difference at {path}: {d1} != {d2}");
                result.AreEqual = false;

                return;
            }

            if (!obj1.Equals(obj2))
            {
                result.Differences.Add($"Value difference at {path}: {obj1} != {obj2}");
                result.AreEqual = false;
            }
        }

        private void CompareCollections(object? obj1, object? obj2, string path,
            ComparisonResult result, Stack<(object? Obj1, object? Obj2, string Path, int Depth)> stack,
            int depth, TypeMetadata metadata)
        {
            if (obj1 == null || obj2 == null)
            {
                result.Differences.Add($"Null collection at {path}");
                result.AreEqual = false;
                return;
            }

            try
            {
                var collection1 = (IEnumerable)obj1;
                var collection2 = (IEnumerable)obj2;

                var list1 = collection1.Cast<object>().ToList();
                var list2 = collection2.Cast<object>().ToList();

                if (list1.Count != list2.Count)
                {
                    result.Differences.Add($"Collection length difference at {path}: {list1.Count} != {list2.Count}");
                    result.AreEqual = false;
                    return;
                }

                // Check if we have a custom comparer for the collection items
                if (metadata.ItemType != null &&
                    _config.CollectionItemComparers.TryGetValue(metadata.ItemType, out var itemComparer))
                {
                    if (_config.IgnoreCollectionOrder)
                    {
                        CompareUnorderedCollectionsWithComparer(list1, list2, path, result, itemComparer);
                    }
                    else
                    {
                        CompareOrderedCollectionsWithComparer(list1, list2, path, result, itemComparer);
                    }
                }
                else if (_config.IgnoreCollectionOrder)
                {
                    if (metadata.ItemType != null && IsSimpleType(metadata.ItemType))
                    {
                        CompareUnorderedCollectionsFast(list1, list2, path, result);
                    }
                    else
                    {
                        CompareUnorderedCollectionsSlow(list1, list2, path, result);
                    }
                }
                else
                {
                    CompareOrderedCollections(list1, list2, path, stack, depth);
                }
            }
            catch (Exception ex)
            {
                throw new ComparisonException($"Failed to compare collections at {path}", path, ex);
            }
        }

        private static void CompareUnorderedCollectionsWithComparer(List<object> list1, List<object> list2,
            string path, ComparisonResult result, IEqualityComparer itemComparer)
        {
            var unmatchedItems2 = new List<object>(list2);

            for (var i = 0; i < list1.Count; i++)
            {
                var item1 = list1[i];
                var matchFound = false;

                for (var j = unmatchedItems2.Count - 1; j >= 0; j--)
                {
                    if (!itemComparer.Equals(item1, unmatchedItems2[j])) continue;
                    
                    unmatchedItems2.RemoveAt(j);
                    matchFound = true;
                    break;
                }

                if (matchFound) continue;
                
                result.Differences.Add($"No matching item found in collection at {path}[{i}]");
                result.AreEqual = false;
                return;
            }
        }

        private static void CompareUnorderedCollectionsFast(List<object> list1, List<object> list2,
            string path, ComparisonResult result)
        {
            var counts1 = new Dictionary<object, int>(new FastEqualityComparer());
            var counts2 = new Dictionary<object, int>(new FastEqualityComparer());

            foreach (var item in list1)
            {
                counts1.TryGetValue(item, out var count);
                counts1[item] = count + 1;
            }

            foreach (var item in list2)
            {
                counts2.TryGetValue(item, out var count);
                counts2[item] = count + 1;
            }

            foreach (var kvp in counts1)
            {
                if (counts2.TryGetValue(kvp.Key, out var count2) && count2 == kvp.Value) continue;
                
                result.Differences.Add($"Collection item count mismatch at {path}");
                result.AreEqual = false;
                return;
            }
        }

        private void CompareUnorderedCollectionsSlow(List<object> list1, List<object> list2,
            string path, ComparisonResult result)
        {
            var matched = new bool[list2.Count];

            for (var i = 0; i < list1.Count; i++)
            {
                var item1 = list1[i];
                var matchFound = false;

                for (var j = 0; j < list2.Count; j++)
                {
                    if (matched[j]) continue;

                    var tempResult = new ComparisonResult();
                    CompareObjectsIterative(item1, list2[j], $"{path}[{i}]", tempResult, new ComparisonContext());

                    if (!tempResult.AreEqual) continue;
                    
                    matched[j] = true;
                    matchFound = true;
                    break;
                }

                if (matchFound) continue;
                
                result.Differences.Add($"No matching item found in collection at {path}[{i}]");
                result.AreEqual = false;
                return;
            }
        }

        private static void CompareOrderedCollectionsWithComparer(List<object> list1, List<object> list2,
            string path, ComparisonResult result, IEqualityComparer itemComparer)
        {
            for (var i = 0; i < list1.Count; i++)
            {
                if (itemComparer.Equals(list1[i], list2[i])) continue;
                
                result.Differences.Add($"Collection items differ at {path}[{i}]");
                result.AreEqual = false;
                return;
            }
        }

        private static void CompareOrderedCollections(List<object> list1, List<object> list2,
            string path,
            Stack<(object? Obj1, object? Obj2, string Path, int Depth)> stack, int depth)
        {
            for (var i = 0; i < list1.Count; i++)
            {
                stack.Push((list1[i], list2[i], $"{path}[{i}]", depth + 1));
            }
        }

        private bool ShouldCompareProperty(PropertyInfo prop)
        {
            ArgumentNullException.ThrowIfNull(prop);

            if (_config.ExcludedProperties.Contains(prop.Name)) return false;
            if (!prop.CanRead) return false;
            if (!_config.CompareReadOnlyProperties && !prop.CanWrite) return false;
            return true;
        }

        private bool IsSimpleType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return TypeCache.GetMetadata(type, _config.UseCachedMetadata).IsSimpleType;
        }
    }
}