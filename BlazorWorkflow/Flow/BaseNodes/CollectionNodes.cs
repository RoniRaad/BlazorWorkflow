using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BlazorWorkflow.Flow.Attributes;
using BlazorWorkflow.Models;
using BlazorWorkflow.Models.NodeV2;

namespace BlazorWorkflow.Flow.BaseNodes
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
    }
}
