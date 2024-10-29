using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace ObjectComparison
{
    /// <summary>
    /// Main object comparer class with optimized implementation
    /// </summary>
    public partial class ObjectComparer
    {
        private readonly ComparisonConfig _config;
        private readonly ExpressionCloner _cloner;

        public ObjectComparer(ComparisonConfig? config = null)
        {
            _config = config ?? new ComparisonConfig();
            _cloner = new ExpressionCloner(_config);
        }

        /// <summary>
        /// Takes a snapshot of an object for later comparison
        /// </summary>
        public T TakeSnapshot<T>(T obj)
        {
            return _cloner.Clone(obj);
        }

        /// <summary>
        /// Compares two objects and returns detailed results
        /// </summary>
        public ComparisonResult Compare<T>(T obj1, T obj2)
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
    }

    public partial class ObjectComparer
    {
        private void CompareObjectsIterative(object obj1, object obj2, string path,
            ComparisonResult result, ComparisonContext context)
        {
            var stack = new Stack<(object, object, string, int)>();
            stack.Push((obj1, obj2, path, 0));

            while (stack.Count > 0 && context.ObjectsCompared < _config.MaxObjectCount)
            {
                var (current1, current2, currentPath, depth) = stack.Pop();
                context.PushObject(current1);

                try
                {
                    if (depth >= _config.MaxDepth)
                    {
                        result.MaxDepthPath = currentPath;
                        continue;
                    }

                    if (HandleNulls(current1, current2, currentPath, result))
                    {
                        continue;
                    }

                    var type = current1?.GetType() ?? current2?.GetType();
                    var metadata = TypeCache.GetMetadata(type, _config.UseCachedMetadata);

                    // Handle circular references
                    var pair = new ComparisonContext.ComparisonPair(current1, current2);
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
                    context.PopObject();
                }
            }

            if (context.ObjectsCompared >= _config.MaxObjectCount)
            {
                result.Differences.Add(
                    $"Comparison aborted: exceeded maximum object count of {_config.MaxObjectCount}");
                result.AreEqual = false;
            }
        }

        private bool HandleNulls(object obj1, object obj2, string path, ComparisonResult result)
        {
            if (ReferenceEquals(obj1, obj2)) return true;

            if (obj1 == null || obj2 == null)
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

        private void HandleCustomComparison(ICustomComparer comparer, object obj1, object obj2,
            string path, ComparisonResult result)
        {
            try
            {
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

        private void CompareNullableTypes(object obj1, object obj2, string path,
            ComparisonResult result, TypeMetadata metadata)
        {
            try
            {
                var value1 = obj1 != null ? TypeCache.GetPropertyGetter(obj1.GetType(), "Value")(obj1) : null;
                var value2 = obj2 != null ? TypeCache.GetPropertyGetter(obj2.GetType(), "Value")(obj2) : null;

                if (metadata.UnderlyingType == typeof(decimal))
                {
                    CompareDecimals(value1 as decimal?, value2 as decimal?, path, result);
                    return;
                }

                if (metadata.HasCustomEquality && metadata.EqualityComparer != null)
                {
                    if (!metadata.EqualityComparer(value1, value2))
                    {
                        result.Differences.Add($"Nullable value difference at {path}: {value1} != {value2}");
                        result.AreEqual = false;
                    }

                    return;
                }

                if (!Equals(value1, value2))
                {
                    result.Differences.Add($"Nullable value difference at {path}: {value1} != {value2}");
                    result.AreEqual = false;
                }
            }
            catch (Exception ex)
            {
                throw new ComparisonException($"Failed to compare nullable values at {path}", path, ex);
            }
        }

        private void CompareSimpleTypes(object obj1, object obj2, string path,
            ComparisonResult result, TypeMetadata metadata)
        {
            if (metadata.HasCustomEquality && metadata.EqualityComparer != null)
            {
                if (!metadata.EqualityComparer(obj1, obj2))
                {
                    result.Differences.Add($"Value difference at {path}: {obj1} != {obj2}");
                    result.AreEqual = false;
                }

                return;
            }

            if (obj1 is decimal dec1 && obj2 is decimal dec2)
            {
                CompareDecimals(dec1, dec2, path, result);
                return;
            }

            if (obj1 is float f1 && obj2 is float f2)
            {
                CompareFloats(f1, f2, path, result);
                return;
            }

            if (obj1 is double d1 && obj2 is double d2)
            {
                CompareDoubles(d1, d2, path, result);
                return;
            }

            if (!obj1.Equals(obj2))
            {
                result.Differences.Add($"Value difference at {path}: {obj1} != {obj2}");
                result.AreEqual = false;
            }
        }

        private void CompareDecimals(decimal? dec1, decimal? dec2, string path, ComparisonResult result)
        {
            var value1 = dec1 ?? 0m;
            var value2 = dec2 ?? 0m;
            var rounded1 = Math.Round(value1, _config.DecimalPrecision);
            var rounded2 = Math.Round(value2, _config.DecimalPrecision);

            if (rounded1 != rounded2)
            {
                result.Differences.Add($"Decimal difference at {path}: {rounded1} != {rounded2}");
                result.AreEqual = false;
            }
        }

        private void CompareFloats(float f1, float f2, string path, ComparisonResult result)
        {
            if (Math.Abs(f1 - f2) > float.Epsilon)
            {
                result.Differences.Add($"Float difference at {path}: {f1} != {f2}");
                result.AreEqual = false;
            }
        }

        private void CompareDoubles(double d1, double d2, string path, ComparisonResult result)
        {
            if (Math.Abs(d1 - d2) > double.Epsilon)
            {
                result.Differences.Add($"Double difference at {path}: {d1} != {d2}");
                result.AreEqual = false;
            }
        }

        private void CompareCollections(object obj1, object obj2, string path,
            ComparisonResult result, Stack<(object, object, string, int)> stack,
            int depth, TypeMetadata metadata)
        {
            try
            {
                var collection1 = (IEnumerable)obj1;
                var collection2 = (IEnumerable)obj2;

                var list1 = collection1?.Cast<object>().ToList() ?? new List<object>();
                var list2 = collection2?.Cast<object>().ToList() ?? new List<object>();

                if (list1.Count != list2.Count)
                {
                    result.Differences.Add($"Collection length difference at {path}: {list1.Count} != {list2.Count}");
                    result.AreEqual = false;
                    return;
                }

                // Check if we have a custom comparer for the collection items
                var hasCustomItemComparer = _config.CollectionItemComparers.TryGetValue(
                    metadata.ItemType ?? typeof(object),
                    out var itemComparer);

                if (_config.IgnoreCollectionOrder)
                {
                    if (hasCustomItemComparer)
                    {
                        CompareUnorderedCollectionsWithComparer(list1, list2, path, result, itemComparer);
                    }
                    else if (metadata.ItemType != null && IsSimpleType(metadata.ItemType))
                    {
                        CompareUnorderedCollectionsFast(list1, list2, path, result);
                    }
                    else
                    {
                        CompareUnorderedCollectionsSlow(list1, list2, path, result, stack, depth);
                    }
                }
                else
                {
                    if (hasCustomItemComparer)
                    {
                        CompareOrderedCollectionsWithComparer(list1, list2, path, result, itemComparer);
                    }
                    else
                    {
                        CompareOrderedCollections(list1, list2, path, result, stack, depth);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ComparisonException($"Failed to compare collections at {path}", path, ex);
            }
        }

        private void CompareUnorderedCollectionsWithComparer(List<object> list1, List<object> list2,
            string path, ComparisonResult result, IEqualityComparer itemComparer)
        {
            var unmatchedItems2 = new List<object>(list2);

            for (var i = 0; i < list1.Count; i++)
            {
                var item1 = list1[i];
                var matchFound = false;

                for (var j = unmatchedItems2.Count - 1; j >= 0; j--)
                {
                    if (itemComparer.Equals(item1, unmatchedItems2[j]))
                    {
                        unmatchedItems2.RemoveAt(j);
                        matchFound = true;
                        break;
                    }
                }

                if (!matchFound)
                {
                    result.Differences.Add($"No matching item found in collection at {path}[{i}]");
                    result.AreEqual = false;
                    return;
                }
            }
        }

        private void CompareUnorderedCollectionsFast(List<object> list1, List<object> list2,
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
                if (!counts2.TryGetValue(kvp.Key, out var count2) || count2 != kvp.Value)
                {
                    result.Differences.Add($"Collection item count mismatch at {path}");
                    result.AreEqual = false;
                    return;
                }
            }
        }

        private void CompareUnorderedCollectionsSlow(List<object> list1, List<object> list2,
            string path, ComparisonResult result, Stack<(object, object, string, int)> stack, int depth)
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
                    var tempStack = new Stack<(object, object, string, int)>();
                    tempStack.Push((item1, list2[j], $"{path}[{i}]", depth + 1));

                    while (tempStack.Count > 0)
                    {
                        var (tempObj1, tempObj2, tempPath, tempDepth) = tempStack.Pop();
                        CompareObjectsIterative(tempObj1, tempObj2, tempPath, tempResult, new ComparisonContext());
                    }

                    if (tempResult.AreEqual)
                    {
                        matched[j] = true;
                        matchFound = true;
                        break;
                    }
                }

                if (!matchFound)
                {
                    result.Differences.Add($"No matching item found in collection at {path}[{i}]");
                    result.AreEqual = false;
                    return;
                }
            }
        }

        private void CompareOrderedCollectionsWithComparer(List<object> list1, List<object> list2,
            string path, ComparisonResult result, IEqualityComparer itemComparer)
        {
            for (var i = 0; i < list1.Count; i++)
            {
                if (!itemComparer.Equals(list1[i], list2[i]))
                {
                    result.Differences.Add($"Collection items differ at {path}[{i}]");
                    result.AreEqual = false;
                    return;
                }
            }
        }

        private void CompareOrderedCollections(List<object> list1, List<object> list2,
            string path, ComparisonResult result, Stack<(object, object, string, int)> stack, int depth)
        {
            for (var i = 0; i < list1.Count; i++)
            {
                stack.Push((list1[i], list2[i], $"{path}[{i}]", depth + 1));
            }
        }

        private void CompareComplexObjects(object obj1, object obj2, string path,
            ComparisonResult result, TypeMetadata metadata, Stack<(object, object, string, int)> stack, int depth)
        {
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

            // Compare fields if configured
            if (_config.ComparePrivateFields)
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
        }

        private bool ShouldCompareProperty(PropertyInfo prop)
        {
            if (_config.ExcludedProperties.Contains(prop.Name)) return false;
            if (!prop.CanRead) return false;
            if (!_config.CompareReadOnlyProperties && !prop.CanWrite) return false;
            return true;
        }

        private bool AreValuesEqual(object value1, object value2)
        {
            if (ReferenceEquals(value1, value2)) return true;
            if (value1 == null || value2 == null) return false;
            return value1.Equals(value2);
        }

        private bool IsEmpty(object obj)
        {
            if (obj == null) return true;
            if (obj is string str) return string.IsNullOrEmpty(str);
            if (obj is IEnumerable enumerable) return !enumerable.Cast<object>().Any();
            return false;
        }

        private bool IsSimpleType(Type type)
        {
            return TypeCache.GetMetadata(type, _config.UseCachedMetadata).IsSimpleType;
        }

        private class FastEqualityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x == null || y == null) return false;
                return x.Equals(y);
            }

            public int GetHashCode(object obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }
    }
}