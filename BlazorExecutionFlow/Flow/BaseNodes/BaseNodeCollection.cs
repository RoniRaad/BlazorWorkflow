using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Flow.Attributes;
using BlazorExecutionFlow.Helpers;

namespace BlazorExecutionFlow.Flow.BaseNodes
{
    /// <summary>
    /// Additional "nice to have" nodes for extended functionality.
    /// These nodes provide math, string, parsing, random, date/time, and other utility operations.
    /// Core workflow nodes are in CoreNodes.cs, HTTP nodes are in HttpNodes.cs.
    /// </summary>
    public static class BaseNodeCollection
    {
        // ---------- Arithmetic (int) ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Add(int input1, int input2) => input1 + input2;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Subtract(int input1, int input2) => input1 - input2;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Multiply(int input1, int input2) => input1 * input2;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Divide(int numerator, int denominator)
        {
            if (denominator == 0) throw new DivideByZeroException("Denominator cannot be zero.");
            return numerator / denominator;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Modulo(int input1, int input2)
        {
            if (input2 == 0) throw new DivideByZeroException("Modulo by zero is not allowed.");
            return input1 % input2;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Min(int a, int b) => Math.Min(a, b);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Max(int a, int b) => Math.Max(a, b);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Clamp(int value, int min, int max)
        {
            if (min > max) (min, max) = (max, min);
            return Math.Min(Math.Max(value, min), max);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Abs(int value) => Math.Abs(value);

        // Map value from one range to another
        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static double MapRange(
            double value,
            [BlazorFlowInputField] double inMin,
            [BlazorFlowInputField] double inMax,
            [BlazorFlowInputField] double outMin,
            [BlazorFlowInputField] double outMax)
        {
            if (Math.Abs(inMax - inMin) < double.Epsilon)
                return outMin;
            var t = (value - inMin) / (inMax - inMin);
            return outMin + t * (outMax - outMin);
        }

        // ---------- Floating-point helpers ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double AddD(double input1, double input2) => input1 + input2;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double SubtractD(double input1, double input2) => input1 - input2;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double MultiplyD(double input1, double input2) => input1 * input2;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double DivideD(double numerator, double denominator)
        {
            if (Math.Abs(denominator) < double.Epsilon)
                throw new DivideByZeroException("Denominator cannot be zero.");
            return numerator / denominator;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double Pow(double @base, double exponent) => Math.Pow(@base, exponent);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double Sqrt(double value) => Math.Sqrt(value);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double Sin(double value) => Math.Sin(value);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double Cos(double value) => Math.Cos(value);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double Tan(double value) => Math.Tan(value);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double ClampD(double value, double min, double max)
        {
            if (min > max) (min, max) = (max, min);
            return Math.Min(Math.Max(value, min), max);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double AbsD(double value) => Math.Abs(value);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double RoundD(double value, int digits = 0) => Math.Round(value, digits);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double FloorD(double value) => Math.Floor(value);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double CeilingD(double value) => Math.Ceiling(value);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math/Float")]
        public static double Lerp(double a, double b, double t)
            => a + (b - a) * ClampD(t, 0.0, 1.0);

        // ---------- Strings ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string StringConcat(string input1, string input2)
            => (input1 ?? "") + (input2 ?? "");

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string JoinWith(string input1, string input2, [BlazorFlowInputField] string separator = "")
            => string.Join(separator ?? string.Empty, input1 ?? string.Empty, input2 ?? string.Empty);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string JoinArray(JsonArray items, [BlazorFlowInputField] string separator = ",")
        {
            if (items == null || items.Count == 0) return string.Empty;
            var stringValues = items.Select(node => node?.ToString() ?? string.Empty);
            return string.Join(separator ?? string.Empty, stringValues);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string ToUpper(string input) => input?.ToUpperInvariant() ?? string.Empty;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string ToLower(string input) => input?.ToLowerInvariant() ?? string.Empty;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string Trim(string input) => input?.Trim() ?? string.Empty;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static int Length(string input) => input?.Length ?? 0;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static bool Contains(string input, string value, [BlazorFlowInputField] bool ignoreCase = false)
        {
            if (input == null || value == null) return false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return input.IndexOf(value, comparison) >= 0;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static bool StartsWith(string input, string value, [BlazorFlowInputField] bool ignoreCase = false)
        {
            if (input == null || value == null) return false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return input.StartsWith(value, comparison);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static bool EndsWith(string input, string value, [BlazorFlowInputField] bool ignoreCase = false)
        {
            if (input == null || value == null) return false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return input.EndsWith(value, comparison);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string Substring(string input, int startIndex, int length)
        {
            if (input == null) return string.Empty;
            if (startIndex < 0) startIndex = 0;
            if (startIndex > input.Length) return string.Empty;
            if (length < 0) length = 0;
            if (startIndex + length > input.Length) length = input.Length - startIndex;
            return input.Substring(startIndex, length);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string Replace(string input, string oldValue, string newValue)
            => (input ?? string.Empty).Replace(oldValue ?? string.Empty, newValue ?? string.Empty);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string[] Split(string input, [BlazorFlowInputField] string separator)
        {
            if (input == null) return Array.Empty<string>();
            separator ??= ",";
            return input.Split(separator, StringSplitOptions.None);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static int IndexOf(string input, string value, [BlazorFlowInputField] bool ignoreCase = false)
        {
            if (input == null || value == null) return -1;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return input.IndexOf(value, comparison);
        }


        // ---------- Parsing / Conversion ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static int ParseInt(string text, [BlazorFlowInputField] int @default = 0)
            => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : @default;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static double ParseDouble(string text, [BlazorFlowInputField] double @default = 0.0)
            => double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v) ? v : @default;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static bool ParseBool(string text, [BlazorFlowInputField] bool @default = false)
            => bool.TryParse(text, out var v) ? v : @default;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static DateTime ParseDateTime(
            string text,
            [BlazorFlowInputField] string format,
            [BlazorFlowInputField] bool assumeUtc = true)
        {
            if (string.IsNullOrWhiteSpace(text))
                return DateTime.MinValue;

            if (!string.IsNullOrWhiteSpace(format) &&
                DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dtExact))
            {
                return assumeUtc ? DateTime.SpecifyKind(dtExact, DateTimeKind.Utc) : dtExact;
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return assumeUtc ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt;

            return DateTime.MinValue;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static string ToStringInvariant(double value) => value.ToString(CultureInfo.InvariantCulture);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static string IntToString(int value) => value.ToString(CultureInfo.InvariantCulture);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static double IntToDouble(int value) => value;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static int DoubleToInt(double value) => (int)value;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static int BoolToInt(bool value) => value ? 1 : 0;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Parsing")]
        public static bool IntToBool(int value) => value != 0;

        // ---------- Random ----------

        private static readonly Random _random = new();

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Utility")]
        public static int RandomInteger() => _random.Next();

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Utility")]
        public static int RandomIntegerRange([BlazorFlowInputField] int min, [BlazorFlowInputField] int max)
        {
            if (min == max) return min;
            if (min > max) (min, max) = (max, min);
            return _random.Next(min, max);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Utility")]
        public static double RandomDouble() => _random.NextDouble();

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Utility")]
        public static bool RandomBool() => _random.Next(0, 2) == 1;

        // ---------- Date/Time ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime UtcNow() => DateTime.UtcNow;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime NowLocal() => DateTime.Now;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime AddSeconds(DateTime dateTime, double seconds) => dateTime.AddSeconds(seconds);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime AddMilliseconds(DateTime dateTime, double milliseconds) => dateTime.AddMilliseconds(milliseconds);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime AddDays(DateTime dateTime, double days) => dateTime.AddDays(days);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static string FormatIso8601(DateTime dateTime)
            => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static long ToUnixSeconds(DateTime dateTime)
            => new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static DateTime FromUnixSeconds(long seconds)
            => DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static double DateDiffDays(DateTime startDate, DateTime endDate)
        {
            return (endDate - startDate).TotalDays;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static double DateDiffHours(DateTime startDate, DateTime endDate)
        {
            return (endDate - startDate).TotalHours;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Date/Time")]
        public static string FormatDateTime(DateTime dateTime, [BlazorFlowInputField] string format = "yyyy-MM-dd HH:mm:ss")
        {
            return dateTime.ToString(format);
        }

        // ---------- JSON helpers (for payload work) ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "JSON")]
        public static JsonObject JsonMerge(JsonObject? left, JsonObject? right)
        {
            var result = new JsonObject();
            if (left != null) result.Merge(left);
            if (right != null) result.Merge(right);
            return result;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "JSON")]
        public static JsonNode? JsonGet(JsonObject? obj, [BlazorFlowInputField] string path)
        {
            if (obj == null || string.IsNullOrWhiteSpace(path)) return null;
            return obj.GetByPath(path);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "JSON")]
        public static JsonObject JsonSet(JsonObject? obj, [BlazorFlowInputField] string path, JsonNode? value)
        {
            obj ??= new JsonObject();
            var clone = new JsonObject();
            clone.Merge(obj);
            if (!string.IsNullOrWhiteSpace(path))
            {
                clone.SetByPath(path, value);
            }
            return clone;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "JSON")]
        public static JsonObject JsonSetString(JsonObject? obj, [BlazorFlowInputField] string path, [BlazorFlowInputField] string value)
        {
            obj ??= new JsonObject();
            var clone = new JsonObject();
            clone.Merge(obj);
            if (!string.IsNullOrWhiteSpace(path))
            {
                clone.SetByPath(path, value);
            }
            return clone;
        }

        // ---------- Math Utilities ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static double Average(JsonArray numbers)
        {
            if (numbers == null || numbers.Count == 0) return 0;
            var values = numbers.Select(n => n?.GetValue<double>() ?? 0).ToArray();
            return values.Length > 0 ? values.Average() : 0;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static double Sum(JsonArray numbers)
        {
            if (numbers == null || numbers.Count == 0) return 0;
            var values = numbers.Select(n => n?.GetValue<double>() ?? 0).ToArray();
            return values.Sum();
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static double AbsDiff(double a, double b)
        {
            return Math.Abs(a - b);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static double MinOf(JsonArray numbers)
        {
            if (numbers == null || numbers.Count == 0) return 0;
            var values = numbers.Select(n => n?.GetValue<double>() ?? 0).ToArray();
            return values.Length > 0 ? values.Min() : 0;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static double MaxOf(JsonArray numbers)
        {
            if (numbers == null || numbers.Count == 0) return 0;
            var values = numbers.Select(n => n?.GetValue<double>() ?? 0).ToArray();
            return values.Length > 0 ? values.Max() : 0;
        }
    }
}
