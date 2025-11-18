using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BlazorExecutionFlow.Flow.Attributes;
using BlazorExecutionFlow.Models;
using BlazorExecutionFlow.Models.NodeV2;

namespace BlazorExecutionFlow.Flow.BaseNodes
{
    /// <summary>
    /// Collection processing nodes for working with arrays and lists.
    /// These nodes allow transforming, filtering, and aggregating collections without explicit loops.
    /// </summary>
    public static class CollectionNodes
    {
        // ==========================================
        // BASIC COLLECTION OPERATIONS
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static int CountStrings(List<string> collection)
        {
            return collection?.Count ?? 0;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static int CountNumbers(List<int> collection)
        {
            return collection?.Count ?? 0;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static string GetStringAtIndex(List<string> collection, int index)
        {
            if (collection == null || index < 0 || index >= collection.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range");

            return collection[index];
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static int GetNumberAtIndex(List<int> collection, int index)
        {
            if (collection == null || index < 0 || index >= collection.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range");

            return collection[index];
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> CreateStringList(string item1, string item2, string item3)
        {
            return new List<string> { item1, item2, item3 };
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> CreateNumberList(int item1, int item2, int item3)
        {
            return new List<int> { item1, item2, item3 };
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> AddString(List<string> collection, string item)
        {
            var result = collection?.ToList() ?? new List<string>();
            result.Add(item);
            return result;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> AddNumber(List<int> collection, int item)
        {
            var result = collection?.ToList() ?? new List<int>();
            result.Add(item);
            return result;
        }

        // ==========================================
        // MAP - Transform each element
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> MapNumbersToString(List<int> collection)
        {
            if (collection == null) return new List<string>();
            return collection.Select(x => x.ToString()).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> MapToLength(List<string> collection)
        {
            if (collection == null) return new List<int>();
            return collection.Select(s => s?.Length ?? 0).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> MapToUpperCase(List<string> collection)
        {
            if (collection == null) return new List<string>();
            return collection.Select(s => s?.ToUpper() ?? "").ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> MapToLowerCase(List<string> collection)
        {
            if (collection == null) return new List<string>();
            return collection.Select(s => s?.ToLower() ?? "").ToList();
        }

        // ==========================================
        // FILTER - Select elements that match condition
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> FilterGreaterThan(List<int> collection, int threshold)
        {
            if (collection == null) return new List<int>();
            return collection.Where(x => x > threshold).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> FilterLessThan(List<int> collection, int threshold)
        {
            if (collection == null) return new List<int>();
            return collection.Where(x => x < threshold).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> FilterContains(List<string> collection, string substring)
        {
            if (collection == null) return new List<string>();
            return collection.Where(s => s?.Contains(substring, StringComparison.OrdinalIgnoreCase) ?? false).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> FilterNotEmpty(List<string> collection)
        {
            if (collection == null) return new List<string>();
            return collection.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }

        // ==========================================
        // REDUCE/AGGREGATE - Combine elements
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static int Sum(List<int> collection)
        {
            if (collection == null) return 0;
            return collection.Sum();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static double Average(List<int> collection)
        {
            if (collection == null || collection.Count == 0) return 0;
            return collection.Average();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static int Max(List<int> collection)
        {
            if (collection == null || collection.Count == 0)
                throw new InvalidOperationException("Collection is empty");
            return collection.Max();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static int Min(List<int> collection)
        {
            if (collection == null || collection.Count == 0)
                throw new InvalidOperationException("Collection is empty");
            return collection.Min();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static string Join(List<string> collection, string separator)
        {
            if (collection == null) return "";
            return string.Join(separator, collection);
        }

        // ==========================================
        // UTILITY
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> ReverseStrings(List<string> collection)
        {
            if (collection == null) return new List<string>();
            var result = collection.ToList();
            result.Reverse();
            return result;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> ReverseNumbers(List<int> collection)
        {
            if (collection == null) return new List<int>();
            var result = collection.ToList();
            result.Reverse();
            return result;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> TakeStrings(List<string> collection, int count)
        {
            if (collection == null) return new List<string>();
            return collection.Take(count).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> TakeNumbers(List<int> collection, int count)
        {
            if (collection == null) return new List<int>();
            return collection.Take(count).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> SkipStrings(List<string> collection, int count)
        {
            if (collection == null) return new List<string>();
            return collection.Skip(count).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> SkipNumbers(List<int> collection, int count)
        {
            if (collection == null) return new List<int>();
            return collection.Skip(count).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> DistinctStrings(List<string> collection)
        {
            if (collection == null) return new List<string>();
            return collection.Distinct().ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> DistinctNumbers(List<int> collection)
        {
            if (collection == null) return new List<int>();
            return collection.Distinct().ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> Split(string input, string separator)
        {
            if (string.IsNullOrEmpty(input)) return new List<string>();
            return input.Split(separator, StringSplitOptions.None).ToList();
        }

        // ==========================================
        // BATCH PROCESSING (Works with current model)
        // ==========================================

        /// <summary>
        /// Processes all items in a collection and returns the results.
        /// Use this when you need to apply the same operation to all items.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> MultiplyAll(List<int> collection, int multiplier)
        {
            if (collection == null) return new List<int>();
            return collection.Select(x => x * multiplier).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> PrefixAll(List<string> collection, string prefix)
        {
            if (collection == null) return new List<string>();
            return collection.Select(s => prefix + s).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> SuffixAll(List<string> collection, string suffix)
        {
            if (collection == null) return new List<string>();
            return collection.Select(s => s + suffix).ToList();
        }

        // ==========================================
        // RANGE GENERATION
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> Range(int start, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");

            return Enumerable.Range(start, count).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> RangeBetween(int start, int end)
        {
            if (end < start)
                throw new ArgumentException("End must be greater than or equal to start");

            return Enumerable.Range(start, end - start + 1).ToList();
        }

        // ==========================================
        // SORTING
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> SortStrings(List<string> collection)
        {
            if (collection == null) return new List<string>();
            return collection.OrderBy(s => s).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> SortStringsDescending(List<string> collection)
        {
            if (collection == null) return new List<string>();
            return collection.OrderByDescending(s => s).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> SortNumbers(List<int> collection)
        {
            if (collection == null) return new List<int>();
            return collection.OrderBy(n => n).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> SortNumbersDescending(List<int> collection)
        {
            if (collection == null) return new List<int>();
            return collection.OrderByDescending(n => n).ToList();
        }

        // ==========================================
        // ELEMENT ACCESS
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static string FirstString(List<string> collection)
        {
            if (collection == null || collection.Count == 0)
                throw new InvalidOperationException("Collection is empty");
            return collection[0];
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static int FirstNumber(List<int> collection)
        {
            if (collection == null || collection.Count == 0)
                throw new InvalidOperationException("Collection is empty");
            return collection[0];
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static string LastString(List<string> collection)
        {
            if (collection == null || collection.Count == 0)
                throw new InvalidOperationException("Collection is empty");
            return collection[collection.Count - 1];
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static int LastNumber(List<int> collection)
        {
            if (collection == null || collection.Count == 0)
                throw new InvalidOperationException("Collection is empty");
            return collection[collection.Count - 1];
        }

        // ==========================================
        // SEARCH & CHECK
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static bool ContainsString(List<string> collection, string value)
        {
            if (collection == null) return false;
            return collection.Contains(value);
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static bool ContainsNumber(List<int> collection, int value)
        {
            if (collection == null) return false;
            return collection.Contains(value);
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static int IndexOfString(List<string> collection, string value)
        {
            if (collection == null) return -1;
            return collection.IndexOf(value);
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static int IndexOfNumber(List<int> collection, int value)
        {
            if (collection == null) return -1;
            return collection.IndexOf(value);
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static bool IsEmpty(List<string> collection)
        {
            return collection == null || collection.Count == 0;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static bool IsEmptyNumbers(List<int> collection)
        {
            return collection == null || collection.Count == 0;
        }

        // ==========================================
        // CONCATENATE & MERGE
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> ConcatStrings(List<string> first, List<string> second)
        {
            var result = first?.ToList() ?? new List<string>();
            if (second != null)
                result.AddRange(second);
            return result;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> ConcatNumbers(List<int> first, List<int> second)
        {
            var result = first?.ToList() ?? new List<int>();
            if (second != null)
                result.AddRange(second);
            return result;
        }

        // ==========================================
        // ADDITIONAL FILTERS
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> FilterEquals(List<string> collection, string value)
        {
            if (collection == null) return new List<string>();
            return collection.Where(s => s == value).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> FilterEqualsNumber(List<int> collection, int value)
        {
            if (collection == null) return new List<int>();
            return collection.Where(n => n == value).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> FilterNotEquals(List<string> collection, string value)
        {
            if (collection == null) return new List<string>();
            return collection.Where(s => s != value).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> FilterNotEqualsNumber(List<int> collection, int value)
        {
            if (collection == null) return new List<int>();
            return collection.Where(n => n != value).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> FilterEven(List<int> collection)
        {
            if (collection == null) return new List<int>();
            return collection.Where(n => n % 2 == 0).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> FilterOdd(List<int> collection)
        {
            if (collection == null) return new List<int>();
            return collection.Where(n => n % 2 != 0).ToList();
        }

        // ==========================================
        // STRING TRANSFORMATIONS
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> TrimAll(List<string> collection)
        {
            if (collection == null) return new List<string>();
            return collection.Select(s => s?.Trim() ?? "").ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> ReplaceAll(List<string> collection, string oldValue, string newValue)
        {
            if (collection == null) return new List<string>();
            return collection.Select(s => s?.Replace(oldValue, newValue) ?? "").ToList();
        }

        // ==========================================
        // ADDITIONAL MATH OPERATIONS
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> DivideAll(List<int> collection, int divisor)
        {
            if (divisor == 0)
                throw new DivideByZeroException("Cannot divide by zero");

            if (collection == null) return new List<int>();
            return collection.Select(x => x / divisor).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> AddAll(List<int> collection, int value)
        {
            if (collection == null) return new List<int>();
            return collection.Select(x => x + value).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> SubtractAll(List<int> collection, int value)
        {
            if (collection == null) return new List<int>();
            return collection.Select(x => x - value).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> AbsoluteAll(List<int> collection)
        {
            if (collection == null) return new List<int>();
            return collection.Select(x => Math.Abs(x)).ToList();
        }

        // ==========================================
        // COLLECTION MODIFICATION
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> RemoveStringAt(List<string> collection, int index)
        {
            if (collection == null || index < 0 || index >= collection.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range");

            var result = collection.ToList();
            result.RemoveAt(index);
            return result;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> RemoveNumberAt(List<int> collection, int index)
        {
            if (collection == null || index < 0 || index >= collection.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range");

            var result = collection.ToList();
            result.RemoveAt(index);
            return result;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> InsertStringAt(List<string> collection, int index, string value)
        {
            var result = collection?.ToList() ?? new List<string>();

            if (index < 0 || index > result.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range");

            result.Insert(index, value);
            return result;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> InsertNumberAt(List<int> collection, int index, int value)
        {
            var result = collection?.ToList() ?? new List<int>();

            if (index < 0 || index > result.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range");

            result.Insert(index, value);
            return result;
        }

        // ==========================================
        // GENERIC JSON COLLECTIONS
        // For mixed-type arrays and complex objects
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static int CountJson(List<JsonNode> collection)
        {
            return collection?.Count ?? 0;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static JsonNode GetJsonAtIndex(List<JsonNode> collection, int index)
        {
            if (collection == null || index < 0 || index >= collection.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range");

            return collection[index];
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static JsonNode FirstJson(List<JsonNode> collection)
        {
            if (collection == null || collection.Count == 0)
                throw new InvalidOperationException("Collection is empty");
            return collection[0];
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static JsonNode LastJson(List<JsonNode> collection)
        {
            if (collection == null || collection.Count == 0)
                throw new InvalidOperationException("Collection is empty");
            return collection[collection.Count - 1];
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<JsonNode> AddJson(List<JsonNode> collection, JsonNode item)
        {
            var result = collection?.ToList() ?? new List<JsonNode>();
            result.Add(item);
            return result;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<JsonNode> ReverseJson(List<JsonNode> collection)
        {
            if (collection == null) return new List<JsonNode>();
            var result = collection.ToList();
            result.Reverse();
            return result;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<JsonNode> TakeJson(List<JsonNode> collection, int count)
        {
            if (collection == null) return new List<JsonNode>();
            return collection.Take(count).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<JsonNode> SkipJson(List<JsonNode> collection, int count)
        {
            if (collection == null) return new List<JsonNode>();
            return collection.Skip(count).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<JsonNode> DistinctJson(List<JsonNode> collection)
        {
            if (collection == null) return new List<JsonNode>();
            // Compare by serialized JSON to detect duplicates
            return collection
                .GroupBy(n => n?.ToJsonString())
                .Select(g => g.First())
                .ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<JsonNode> SortJson(List<JsonNode> collection)
        {
            if (collection == null) return new List<JsonNode>();
            // Sort by JSON string representation
            return collection.OrderBy(n => n?.ToJsonString()).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<JsonNode> ConcatJson(List<JsonNode> first, List<JsonNode> second)
        {
            var result = first?.ToList() ?? new List<JsonNode>();
            if (second != null)
                result.AddRange(second);
            return result;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static bool IsEmptyJson(List<JsonNode> collection)
        {
            return collection == null || collection.Count == 0;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<JsonNode> RemoveJsonAt(List<JsonNode> collection, int index)
        {
            if (collection == null || index < 0 || index >= collection.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range");

            var result = collection.ToList();
            result.RemoveAt(index);
            return result;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<JsonNode> InsertJsonAt(List<JsonNode> collection, int index, JsonNode value)
        {
            var result = collection?.ToList() ?? new List<JsonNode>();

            if (index < 0 || index > result.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range");

            result.Insert(index, value);
            return result;
        }

        // ==========================================
        // JSON OBJECT PROPERTY ACCESS
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<JsonNode> PluckProperty(List<JsonNode> collection, string propertyName)
        {
            if (collection == null) return new List<JsonNode>();

            return collection.Select(item =>
            {
                if (item is JsonObject obj && obj.TryGetPropertyValue(propertyName, out var value))
                    return value;
                return null;
            }).Where(v => v != null).ToList()!;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<JsonNode> FilterByProperty(List<JsonNode> collection, string propertyName, JsonNode value)
        {
            if (collection == null) return new List<JsonNode>();

            return collection.Where(item =>
            {
                if (item is JsonObject obj && obj.TryGetPropertyValue(propertyName, out var itemValue))
                {
                    return itemValue?.ToJsonString() == value?.ToJsonString();
                }
                return false;
            }).ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<JsonNode> SortByProperty(List<JsonNode> collection, string propertyName)
        {
            if (collection == null) return new List<JsonNode>();

            return collection.OrderBy(item =>
            {
                if (item is JsonObject obj && obj.TryGetPropertyValue(propertyName, out var value))
                    return value?.ToJsonString() ?? "";
                return "";
            }).ToList();
        }

        // ==========================================
        // TYPE CONVERSION
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<JsonNode> StringsToJson(List<string> collection)
        {
            if (collection == null) return new List<JsonNode>();
            return collection.Select(s => JsonSerializer.SerializeToNode(s)).ToList()!;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<JsonNode> NumbersToJson(List<int> collection)
        {
            if (collection == null) return new List<JsonNode>();
            return collection.Select(n => JsonSerializer.SerializeToNode(n)).ToList()!;
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<string> JsonToStrings(List<JsonNode> collection)
        {
            if (collection == null) return new List<string>();
            return collection.Select(n => n?.GetValue<string>() ?? n?.ToJsonString() ?? "").ToList();
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static List<int> JsonToNumbers(List<JsonNode> collection)
        {
            if (collection == null) return new List<int>();
            return collection.Select(n =>
            {
                if (n != null && n.AsValue().TryGetValue<int>(out var intValue))
                    return intValue;
                return 0;
            }).ToList();
        }
    }
}
