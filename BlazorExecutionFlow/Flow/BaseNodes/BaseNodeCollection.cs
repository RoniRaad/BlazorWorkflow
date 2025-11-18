using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BlazorExecutionFlow.Flow.Attributes;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models.NodeV2;

namespace BlazorExecutionFlow.Flow.BaseNodes
{
    public static class BaseNodeCollection
    {
        // ---------- Events / Triggers ----------

        [BlazorFlowNodeMethod(Models.NodeType.Event, "Events")]
        public static void Start() { }


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

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Negate(int value) => -value;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Increment(int value) => value + 1;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Decrement(int value) => value - 1;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Math")]
        public static int Sign(int value) => Math.Sign(value);

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

        // ---------- Comparison (ints / doubles / strings) ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool Equal(int a, int b) => a == b;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool NotEqual(int a, int b) => a != b;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool GreaterThan(int a, int b) => a > b;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool GreaterOrEqual(int a, int b) => a >= b;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool LessThan(int a, int b) => a < b;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool LessOrEqual(int a, int b) => a <= b;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool EqualD(double a, double b, [BlazorFlowInputField] double tolerance = 0.0)
            => Math.Abs(a - b) <= Math.Max(tolerance, 0.0);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Comparison")]
        public static bool StringEquals(string a, string b, [BlazorFlowInputField] bool ignoreCase = false)
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(a ?? string.Empty, b ?? string.Empty, comparison);
        }

        // ---------- Boolean logic ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool And(bool a, bool b) => a && b;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool Or(bool a, bool b) => a || b;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool Xor(bool a, bool b) => a ^ b;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool Not(bool value) => !value;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool CoalesceBool(bool? value, [BlazorFlowInputField] bool @default = false)
            => value ?? @default;

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
        public static string CoalesceString(string primary, [BlazorFlowInputField] string fallback = "")
            => string.IsNullOrEmpty(primary) ? fallback ?? string.Empty : primary;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static int IndexOf(string input, string value, [BlazorFlowInputField] bool ignoreCase = false)
        {
            if (input == null || value == null) return -1;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return input.IndexOf(value, comparison);
        }

        // Simple format: replaces {0}, {1}, {2}
        [BlazorFlowNodeMethod(Models.NodeType.Function, "Strings")]
        public static string Format3(
            [BlazorFlowInputField] string format,
            [BlazorFlowInputField] string arg0,
            [BlazorFlowInputField] string arg1,
            [BlazorFlowInputField] string arg2)
        {
            format ??= string.Empty;
            return string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2);
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

        // ---------- Logging / Debug / Utility ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Utility")]
        public static void Log(string message) => Console.WriteLine(message);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Utility")]
        public static void LogWarning(string message) => Console.WriteLine("[WARN] " + message);

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Utility")]
        public static void LogError(string message) => Console.Error.WriteLine("[ERROR] " + message);

        // Logs the current node's JSON input
        [BlazorFlowNodeMethod(Models.NodeType.Function, "Utility")]
        public static void DumpInput(NodeContext ctx)
        {
            var json = ctx.CurrentNode.Input?.ToJsonString() ?? "{}";
            Console.WriteLine($"[DumpInput:{ctx.CurrentNode.BackingMethod.Name}] {json}");
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Utility")]
        public static async Task Wait([BlazorFlowInputField] int timeMs)
            => await Task.Delay(Math.Max(0, timeMs));

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Utility")]
        public static string NewGuid() => Guid.NewGuid().ToString("D");

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Utility")]
        public static string MachineName() => Environment.MachineName;

        // ---------- Random ----------

        private static readonly Random _random = new();

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Random")]
        public static int RandomInteger() => _random.Next();

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Random")]
        public static int RandomIntegerRange([BlazorFlowInputField] int min, [BlazorFlowInputField] int max)
        {
            if (min == max) return min;
            if (min > max) (min, max) = (max, min);
            return _random.Next(min, max);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Random")]
        public static double RandomDouble() => _random.NextDouble();

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Random")]
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

        // ---------- Variables (constants) ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Variables")]
        public static string StringVariable([BlazorFlowInputField] string constantString) => constantString;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Variables")]
        public static int IntVariable([BlazorFlowInputField] int constantInt) => constantInt;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Variables")]
        public static double DoubleVariable([BlazorFlowInputField] double constantDouble) => constantDouble;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Variables")]
        public static bool BoolVariable([BlazorFlowInputField] bool constantBool) => constantBool;

        // ---------- Collections (string arrays) ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Collections")]
        public static int ArrayLength(JsonArray items) => items?.Count ?? 0;

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Collections")]
        public static JsonNode? ArrayElementOrDefault(JsonArray items, int index)
        {
            if (items == null || items.Count == 0) return null;
            if (index < 0 || index >= items.Count) return null;
            return items[index];
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Collections")]
        public static JsonArray ArrayAppend(JsonArray items, JsonNode? value)
        {
            var result = new JsonArray();
            if (items != null)
            {
                foreach (var item in items)
                {
                    result.Add(item?.DeepClone());
                }
            }
            result.Add(value?.DeepClone());
            return result;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Collections")]
        public static bool ArrayContains(JsonArray items, string value, [BlazorFlowInputField] bool ignoreCase = false)
        {
            if (items == null || items.Count == 0) return false;
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return items.Any(x => string.Equals(x?.ToString() ?? string.Empty, value ?? string.Empty, comparison));
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

        // ---------- Control Flow with ports ----------

        // Basic If node: routes to "true" or "false"
        [BlazorFlowNodeMethod(Models.NodeType.BooleanOperation, "Conditionals")]
        [NodeFlowPorts("true", "false")]
        public static Task If(NodeContext ctx, bool condition)
        {
            var port = condition ? "true" : "false";
            return ctx.ExecutePortAsync(port);
        }

        // IfNullable: true / false / error (null)
        [BlazorFlowNodeMethod(Models.NodeType.BooleanOperation, "Conditionals")]
        [NodeFlowPorts("true", "false", "error")]
        public static async Task IfNullable(NodeContext ctx, bool? condition)
        {
            if (condition is null)
            {
                await ctx.ExecutePortAsync("error");
            }
            else
            {
                await ctx.ExecutePortAsync(condition.Value ? "true" : "false");
            }
        }

        // Gate: only forwards when "open" is true
        [BlazorFlowNodeMethod(Models.NodeType.BooleanOperation, "Conditionals")]
        [NodeFlowPorts("open", "closed")]
        public static async Task Gate(NodeContext ctx, bool open)
        {
            if (open)
            {
                await ctx.ExecutePortAsync("open");
            }
            else
            {
                await ctx.ExecutePortAsync("closed");
            }
        }

        // SwitchInt: 3 cases + default
        [BlazorFlowNodeMethod(Models.NodeType.BooleanOperation, "Conditionals")]
        [NodeFlowPorts("case1", "case2", "case3", "default")]
        public static async Task SwitchInt(
            NodeContext ctx,
            int value,
            [BlazorFlowInputField] int case1,
            [BlazorFlowInputField] int case2,
            [BlazorFlowInputField] int case3)
        {
            if (value == case1)
            {
                await ctx.ExecutePortAsync("case1");
            }
            else if (value == case2)
            {
                await ctx.ExecutePortAsync("case2");
            }
            else if (value == case3)
            {
                await ctx.ExecutePortAsync("case3");
            }
            else
            {
                await ctx.ExecutePortAsync("default");
            }
        }

        // SwitchString: 3 cases + default
        [BlazorFlowNodeMethod(Models.NodeType.BooleanOperation, "Conditionals")]
        [NodeFlowPorts("case1", "case2", "case3", "default")]
        public static async Task SwitchString(
            NodeContext ctx,
            string value,
            [BlazorFlowInputField] string case1,
            [BlazorFlowInputField] string case2,
            [BlazorFlowInputField] string case3,
            [BlazorFlowInputField] bool ignoreCase = false)
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            value ??= string.Empty;
            case1 ??= string.Empty;
            case2 ??= string.Empty;
            case3 ??= string.Empty;

            if (string.Equals(value, case1, comparison))
            {
                await ctx.ExecutePortAsync("case1");
            }
            else if (string.Equals(value, case2, comparison))
            {
                await ctx.ExecutePortAsync("case2");
            }
            else if (string.Equals(value, case3, comparison))
            {
                await ctx.ExecutePortAsync("case3");
            }
            else
            {
                await ctx.ExecutePortAsync("default");
            }
        }

        // For loop: from start (inclusive) to end (exclusive)
        [BlazorFlowNodeMethod(Models.NodeType.Loop, "Loops")]
        [NodeFlowPorts("loop", "done")]
        public static async Task For(NodeContext ctx, int start, int end)
        {
            if (start > end)
            {
                await ctx.ExecutePortAsync("done");
                return;
            }

            for (int i = start; i < end; i++)
            {
                // If you want index data to downstream nodes, you can later
                // also write to JSON payload via separate nodes.
                ctx.Context["index"] = i;
                await ctx.ExecutePortAsync("loop");
            }

            await ctx.ExecutePortAsync("done");
        }

        // Repeat N times
        [BlazorFlowNodeMethod(Models.NodeType.Loop, "Loops")]
        [NodeFlowPorts("loop", "done")]
        public static async Task Repeat(NodeContext ctx, int count)
        {
            if (count < 0) count = 0;

            for (int i = 0; i < count; i++)
            {
                ctx.Context["index"] = i;
                await ctx.ExecutePortAsync("loop");
            }

            await ctx.ExecutePortAsync("done");
        }

        // While: calls "loop" while condition is true, then "done"
        [BlazorFlowNodeMethod(Models.NodeType.Loop, "Loops")]
        [NodeFlowPorts("loop", "done")]
        public static async Task While(NodeContext ctx, bool condition)
        {
            // This just branches once; to build a real while, connect "loop"
            // back into a path that recomputes the condition and re-enters this node.
            if (condition)
            {
                await ctx.ExecutePortAsync("loop");
            }
            else
            {
                await ctx.ExecutePortAsync("done");
            }
        }

        // ---------- Simple templating (for quick tests) ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Templates")]
        public static string InterpolateString(
            [BlazorFlowInputField] string template,
            [BlazorFlowInputField] string value)
        {
            template ??= string.Empty;
            value ??= string.Empty;
            return template.Replace("{{value}}", value, StringComparison.Ordinal);
        }

        // ---------- HTTP ----------
        private static readonly HttpClient _httpClient = new();

        [BlazorFlowNodeMethod(Models.NodeType.Function, "HTTP")]
        public static async Task<string> HttpGetString(
            [BlazorFlowInputField] string url,
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                var response = await _httpClient.GetAsync(url, cts.Token);
                return await response.Content.ReadAsStringAsync(cts.Token);
            }
            catch (Exception ex)
            {
                LogError($"HttpGetString failed: {ex.Message}");
                return string.Empty;
            }
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "HTTP")]
        public static async Task<int> HttpGetStatusCode(
            [BlazorFlowInputField] string url,
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return 0;

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                var response = await _httpClient.GetAsync(url, cts.Token);
                return (int)response.StatusCode;
            }
            catch (Exception ex)
            {
                LogError($"HttpGetStatusCode failed: {ex.Message}");
                return 0;
            }
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "HTTP")]
        public static async Task<string> HttpPostString(
            [BlazorFlowInputField] string url,
            string body,
            [BlazorFlowInputField] string contentType = "application/json",
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            body ??= string.Empty;
            contentType ??= "application/json";

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                using var content = new StringContent(body, Encoding.UTF8, contentType);
                var response = await _httpClient.PostAsync(url, content, cts.Token);
                return await response.Content.ReadAsStringAsync(cts.Token);
            }
            catch (Exception ex)
            {
                LogError($"HttpPostString failed: {ex.Message}");
                return string.Empty;
            }
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "HTTP")]
        public static async Task<int> HttpPostStatusCode(
            [BlazorFlowInputField] string url,
            string body,
            [BlazorFlowInputField] string contentType = "application/json",
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
                return 0;

            body ??= string.Empty;
            contentType ??= "application/json";

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                using var content = new StringContent(body, Encoding.UTF8, contentType);
                var response = await _httpClient.PostAsync(url, content, cts.Token);
                return (int)response.StatusCode;
            }
            catch (Exception ex)
            {
                LogError($"HttpPostStatusCode failed: {ex.Message}");
                return 0;
            }
        }

        // Flow-style GET: routes to "success" or "error" and also returns the body.
        [BlazorFlowNodeMethod(Models.NodeType.Function, "HTTP")]
        [NodeFlowPorts("success", "error")]
        public static async Task<string> HttpGetFlow(
            NodeContext ctx,
            [BlazorFlowInputField] string url,
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                await ctx.ExecutePortAsync("error");
                return string.Empty;
            }

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                var response = await _httpClient.GetAsync(url, cts.Token);
                var body = await response.Content.ReadAsStringAsync(cts.Token);

                // You can still branch based on success
                if (response.IsSuccessStatusCode)
                {
                    await ctx.ExecutePortAsync("success");
                }
                else
                {
                    await ctx.ExecutePortAsync("error");
                }

                // Returned body will end up as this node's output (output.result)
                return body;
            }
            catch (Exception ex)
            {
                LogError($"HttpGetFlow failed: {ex.Message}");
                await ctx.ExecutePortAsync("error");
                return string.Empty;
            }
        }

        // Flow-style POST: routes to "success" or "error" and returns the body.
        [BlazorFlowNodeMethod(Models.NodeType.Function, "HTTP")]
        [NodeFlowPorts("success", "error")]
        public static async Task<string> HttpPostFlow(
            NodeContext ctx,
            [BlazorFlowInputField] string url,
            string body,
            [BlazorFlowInputField] string contentType = "application/json",
            [BlazorFlowInputField] int timeoutMs = 10000)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                await ctx.ExecutePortAsync("error");
                return string.Empty;
            }

            body ??= string.Empty;
            contentType ??= "application/json";

            using var cts = timeoutMs > 0
                ? new CancellationTokenSource(timeoutMs)
                : new CancellationTokenSource();

            try
            {
                using var content = new StringContent(body, Encoding.UTF8, contentType);
                var response = await _httpClient.PostAsync(url, content, cts.Token);
                var responseBody = await response.Content.ReadAsStringAsync(cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    await ctx.ExecutePortAsync("success");
                }
                else
                {
                    await ctx.ExecutePortAsync("error");
                }

                return responseBody;
            }
            catch (Exception ex)
            {
                LogError($"HttpPostFlow failed: {ex.Message}");
                await ctx.ExecutePortAsync("error");
                return string.Empty;
            }
        }

        // ---------- String Utilities ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "String")]
        public static string Reverse([BlazorFlowInputField] string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            char[] chars = input.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "String")]
        public static string PadLeft([BlazorFlowInputField] string input, int totalWidth, [BlazorFlowInputField] string paddingChar = " ")
        {
            if (string.IsNullOrEmpty(input)) input = string.Empty;
            char pad = string.IsNullOrEmpty(paddingChar) ? ' ' : paddingChar[0];
            return input.PadLeft(totalWidth, pad);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "String")]
        public static string PadRight([BlazorFlowInputField] string input, int totalWidth, [BlazorFlowInputField] string paddingChar = " ")
        {
            if (string.IsNullOrEmpty(input)) input = string.Empty;
            char pad = string.IsNullOrEmpty(paddingChar) ? ' ' : paddingChar[0];
            return input.PadRight(totalWidth, pad);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "String")]
        public static string FormatString([BlazorFlowInputField] string template, [BlazorFlowInputField] string arg0 = "", [BlazorFlowInputField] string arg1 = "", [BlazorFlowInputField] string arg2 = "")
        {
            try
            {
                return string.Format(template, arg0, arg1, arg2);
            }
            catch
            {
                return template;
            }
        }

        // ---------- Array Utilities ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Array")]
        public static string ArrayJoin(JsonArray items, [BlazorFlowInputField] string separator = ",")
        {
            if (items == null || items.Count == 0) return string.Empty;
            var stringValues = items.Select(node => node?.ToString() ?? string.Empty);
            return string.Join(separator, stringValues);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Array")]
        public static JsonNode? ArrayFirst(JsonArray items)
        {
            return items == null || items.Count == 0 ? null : items[0];
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Array")]
        public static JsonNode? ArrayLast(JsonArray items)
        {
            return items == null || items.Count == 0 ? null : items[items.Count - 1];
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Array")]
        public static int ArrayCount(JsonArray items)
        {
            return items?.Count ?? 0;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Array")]
        public static JsonArray ArrayReverse(JsonArray items)
        {
            if (items == null || items.Count == 0) return new JsonArray();
            var reversed = new JsonArray();
            for (int i = items.Count - 1; i >= 0; i--)
            {
                reversed.Add(items[i]?.DeepClone());
            }
            return reversed;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Array")]
        public static JsonNode? ArrayGet(JsonArray items, int index)
        {
            if (items == null || index < 0 || index >= items.Count) return null;
            return items[index];
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Array")]
        public static JsonArray ArraySlice(JsonArray items, int start, int count)
        {
            if (items == null || items.Count == 0) return new JsonArray();
            if (start < 0) start = 0;
            if (start >= items.Count) return new JsonArray();

            int actualCount = Math.Min(count, items.Count - start);
            if (actualCount <= 0) return new JsonArray();

            var result = new JsonArray();
            for (int i = start; i < start + actualCount; i++)
            {
                result.Add(items[i]?.DeepClone());
            }
            return result;
        }

        // ---------- Logic Utilities ----------

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool IsNull(string? input)
        {
            return input == null;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool IsEmpty([BlazorFlowInputField] string? input)
        {
            return string.IsNullOrEmpty(input);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Logic")]
        public static bool IsWhitespace([BlazorFlowInputField] string? input)
        {
            return string.IsNullOrWhiteSpace(input);
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Logic")]
        public static string Ternary(bool condition, [BlazorFlowInputField] string trueValue, [BlazorFlowInputField] string falseValue)
        {
            return condition ? trueValue : falseValue;
        }

        [BlazorFlowNodeMethod(Models.NodeType.Function, "Logic")]
        public static int TernaryInt(bool condition, int trueValue, int falseValue)
        {
            return condition ? trueValue : falseValue;
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

        // ---------- DateTime Utilities ----------

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

    }
}
