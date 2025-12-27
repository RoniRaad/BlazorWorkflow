using System;
using System.Threading.Tasks;
using BlazorWorkflow.Flow.Attributes;
using BlazorWorkflow.Models;
using BlazorWorkflow.Models.NodeV2;
using BlazorWorkflow.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorWorkflow.Flow.BaseNodes
{
    /// <summary>
    /// Core nodes that are essential for basic workflow functionality.
    /// These nodes are always included and provide fundamental workflow capabilities.
    /// </summary>
    public static class CoreNodes
    {
        // ==========================================
        // EVENTS / TRIGGERS
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Event, "Events")]
        public static void Start() { }


        /// <summary>
        /// Prompts the user for input during workflow execution. The workflow will pause until the user provides a value.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "Utility")]
        public static async Task<string> PromptUser(IServiceProvider serviceProvider, [BlazorFlowInputField] string promptMessage, [BlazorFlowInputField] string defaultValue = "")
        {
            if (string.IsNullOrWhiteSpace(promptMessage))
                throw new ArgumentException("Prompt message cannot be empty");

            // Get the prompt service from the service provider
            var promptService = serviceProvider.GetService<IUserPromptService>();
            if (promptService == null)
            {
                throw new InvalidOperationException("User prompt service is not available. Ensure UserPromptService is registered in the DI container.");
            }

            // Request user input and wait for response
            var result = await promptService.PromptUserAsync(promptMessage, defaultValue).ConfigureAwait(false);
            return result ?? defaultValue;
        }

        /// <summary>
        /// Routes execution based on a boolean condition.
        /// Connects to "true" port if condition is true, otherwise "false" port.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.BooleanOperation, "Logic")]
        [NodeFlowPorts("true", "false")]
        public static Task If(NodeContext ctx, bool condition)
        {
            var port = condition ? "true" : "false";
            return ctx.ExecutePortAsync(port);
        }

        // ==========================================
        // LOOPS
        // ==========================================

        /// <summary>
        /// Executes the "loop" port for each iteration from start (inclusive) to end (exclusive).
        /// Then executes the "done" port.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Loop, "Loops")]
        [NodeFlowPorts("loop", "done")]
        public static async Task For(NodeContext ctx, int start, int end)
        {
            if (start > end)
            {
                await ctx.ExecutePortAsync("done").ConfigureAwait(false);
                return;
            }

            for (int i = start; i < end; i++)
            {
                ctx.Context["index"] = i;
                await ctx.ExecutePortAsync("loop").ConfigureAwait(false);
            }

            await ctx.ExecutePortAsync("done").ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the "loop" port N times, then executes the "done" port.
        /// The current iteration index is available in context.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Loop, "Loops")]
        [NodeFlowPorts("loop", "done")]
        public static async Task Repeat(NodeContext ctx, int count)
        {
            if (count < 0) count = 0;

            for (int i = 0; i < count; i++)
            {
                ctx.Context["index"] = i;
                await ctx.ExecutePortAsync("loop").ConfigureAwait(false);
            }

            await ctx.ExecutePortAsync("done").ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the "loop" port if condition is true, otherwise executes "done" port.
        /// For actual while loops, connect the "loop" port back to re-evaluate the condition.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Loop, "Loops")]
        [NodeFlowPorts("loop", "done")]
        public static async Task While(NodeContext ctx, bool condition)
        {
            if (condition)
            {
                await ctx.ExecutePortAsync("loop").ConfigureAwait(false);
            }
            else
            {
                await ctx.ExecutePortAsync("done").ConfigureAwait(false);
            }
        }

        // ==========================================
        // BOOLEAN LOGIC
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Logic")]
        public static bool And(bool a, bool b) => a && b;

        [BlazorFlowNodeMethod(NodeType.Function, "Logic")]
        public static bool Or(bool a, bool b) => a || b;

        [BlazorFlowNodeMethod(NodeType.Function, "Logic")]
        public static bool Xor(bool a, bool b) => a ^ b;

        [BlazorFlowNodeMethod(NodeType.Function, "Logic")]
        public static bool Not(bool value) => !value;

        [BlazorFlowNodeMethod(NodeType.Function, "Logic")]
        public static object? Ternary(bool condition, object? trueValue, object? falseValue)
        {
            return condition ? trueValue : falseValue;
        }

        // ==========================================
        // COMPARISON
        // ==========================================

        /// <summary>
        /// Compares two objects for equality. Handles numbers, strings, nulls, and other types intelligently.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "Logic")]
        public static bool Equal(object? a, object? b)
        {
            // Handle nulls
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            // Try direct equality first
            if (a.Equals(b)) return true;

            // Try converting to comparable types
            try
            {
                // Both numbers - convert to decimal for comparison
                if (IsNumeric(a) && IsNumeric(b))
                {
                    var aDecimal = Convert.ToDecimal(a);
                    var bDecimal = Convert.ToDecimal(b);
                    return aDecimal == bDecimal;
                }

                // String comparison (case-sensitive)
                var aString = a.ToString() ?? "";
                var bString = b.ToString() ?? "";
                // Try one last time to parse as numbers
                if (decimal.TryParse(aString, out var aFinal) && decimal.TryParse(bString, out var bFinal))
                {
                    return aFinal == bFinal;
                }
                return aString.Equals(bString, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Logic")]
        public static bool NotEqual(object? a, object? b) => !Equal(a, b);

        /// <summary>
        /// Checks if first value is greater than second. Works with numbers, dates, and strings.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "Logic")]
        public static bool GreaterThan(object? a, object? b)
        {
            if (a == null || b == null) return false;

            try
            {
                // Try numeric comparison
                if (IsNumeric(a) && IsNumeric(b))
                {
                    var aDecimal = Convert.ToDecimal(a);
                    var bDecimal = Convert.ToDecimal(b);
                    return aDecimal > bDecimal;
                }

                // Try parsing both as numbers if they're strings
                if (a is string aString && b is string bString)
                {
                    if (decimal.TryParse(aString, out var aNum) && decimal.TryParse(bString, out var bNum))
                    {
                        return aNum > bNum;
                    }
                }

                // Try DateTime comparison
                if (a is DateTime dtA && b is DateTime dtB)
                    return dtA > dtB;

                // String comparison
                var aToString = a.ToString() ?? "";
                var bToString = b.ToString() ?? "";
                // Try one last time to parse as numbers
                if (decimal.TryParse(aToString, out var aFinal) && decimal.TryParse(bToString, out var bFinal))
                {
                    return aFinal > bFinal;
                }
                return string.Compare(aToString, bToString, StringComparison.Ordinal) > 0;
            }
            catch
            {
                return false;
            }
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Logic")]
        public static bool GreaterOrEqual(object? a, object? b)
        {
            return Equal(a, b) || GreaterThan(a, b);
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Logic")]
        public static bool LessThan(object? a, object? b)
        {
            return !GreaterOrEqual(a, b);
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Logic")]
        public static bool LessOrEqual(object? a, object? b)
        {
            return !GreaterThan(a, b);
        }

        /// <summary>
        /// Compares two numeric values with a tolerance for floating point precision.
        /// </summary>
        [BlazorFlowNodeMethod(NodeType.Function, "Logic")]
        public static bool EqualWithTolerance(object? a, object? b, [BlazorFlowInputField] double tolerance = 0.0)
        {
            try
            {
                if (a != null && b != null && IsNumeric(a) && IsNumeric(b))
                {
                    var aDouble = Convert.ToDouble(a);
                    var bDouble = Convert.ToDouble(b);
                    return Math.Abs(aDouble - bDouble) <= Math.Max(tolerance, 0.0);
                }
                return Equal(a, b);
            }
            catch
            {
                return false;
            }
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Logic")]
        public static bool StringEquals(string a, string b, [BlazorFlowInputField] bool ignoreCase = false)
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(a ?? string.Empty, b ?? string.Empty, comparison);
        }

        /// <summary>
        /// Helper to check if an object is a numeric type
        /// </summary>
        private static bool IsNumeric(object? obj)
        {
            if (obj == null) return false;
            return obj is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
        }

        // ==========================================
        // UTILITY
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Utility")]
        public static void Log(string message) => Console.WriteLine(message);

        [BlazorFlowNodeMethod(NodeType.Function, "Utility")]
        public static void LogError(string message) => Console.Error.WriteLine("[ERROR] " + message);

        [BlazorFlowNodeMethod(NodeType.Function, "Utility")]
        public static async Task Wait([BlazorFlowInputField] int timeMs)
            => await Task.Delay(Math.Max(0, timeMs)).ConfigureAwait(false);

        [BlazorFlowNodeMethod(NodeType.Function, "Utility")]
        public static string NewGuid() => Guid.NewGuid().ToString("D");
    }
}
