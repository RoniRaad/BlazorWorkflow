using System;
using System.Threading.Tasks;
using BlazorExecutionFlow.Flow.Attributes;
using BlazorExecutionFlow.Models;
using BlazorExecutionFlow.Models.NodeV2;
using BlazorExecutionFlow.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorExecutionFlow.Flow.BaseNodes
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
            var result = await promptService.PromptUserAsync(promptMessage, defaultValue);
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
                await ctx.ExecutePortAsync("done");
                return;
            }

            for (int i = start; i < end; i++)
            {
                ctx.Context["index"] = i;
                await ctx.ExecutePortAsync("loop");
            }

            await ctx.ExecutePortAsync("done");
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
                await ctx.ExecutePortAsync("loop");
            }

            await ctx.ExecutePortAsync("done");
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
                await ctx.ExecutePortAsync("loop");
            }
            else
            {
                await ctx.ExecutePortAsync("done");
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
        public static string Ternary(bool condition, [BlazorFlowInputField] string trueValue, [BlazorFlowInputField] string falseValue)
        {
            return condition ? trueValue : falseValue;
        }

        // ==========================================
        // COMPARISON
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Comparison")]
        public static bool Equal(int a, int b) => a == b;

        [BlazorFlowNodeMethod(NodeType.Function, "Comparison")]
        public static bool NotEqual(int a, int b) => a != b;

        [BlazorFlowNodeMethod(NodeType.Function, "Comparison")]
        public static bool GreaterThan(int a, int b) => a > b;

        [BlazorFlowNodeMethod(NodeType.Function, "Comparison")]
        public static bool GreaterOrEqual(int a, int b) => a >= b;

        [BlazorFlowNodeMethod(NodeType.Function, "Comparison")]
        public static bool LessThan(int a, int b) => a < b;

        [BlazorFlowNodeMethod(NodeType.Function, "Comparison")]
        public static bool LessOrEqual(int a, int b) => a <= b;

        [BlazorFlowNodeMethod(NodeType.Function, "Comparison")]
        public static bool EqualD(double a, double b, [BlazorFlowInputField] double tolerance = 0.0)
            => Math.Abs(a - b) <= Math.Max(tolerance, 0.0);

        [BlazorFlowNodeMethod(NodeType.Function, "Comparison")]
        public static bool StringEquals(string a, string b, [BlazorFlowInputField] bool ignoreCase = false)
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(a ?? string.Empty, b ?? string.Empty, comparison);
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
            => await Task.Delay(Math.Max(0, timeMs));

        [BlazorFlowNodeMethod(NodeType.Function, "Utility")]
        public static string NewGuid() => Guid.NewGuid().ToString("D");
    }
}
