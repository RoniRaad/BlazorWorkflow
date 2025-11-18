using BlazorExecutionFlow.Flow.Attributes;
using BlazorExecutionFlow.Models;
using BlazorExecutionFlow.Models.NodeV2;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorExecutionFlow.Examples
{
    /// <summary>
    /// Example custom node collection showing various node patterns.
    ///
    /// To use this in your application:
    /// 1. Create a similar class in your project
    /// 2. Register it at startup: NodeRegistry.RegisterNodeType<YourCustomNodes>();
    /// 3. Optionally configure services: NodeServiceProvider.ConfigureServiceProvider(app.Services);
    /// </summary>
    public static class CustomNodesExample
    {
        // ==========================================
        // SIMPLE FUNCTION NODES
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Examples")]
        public static string Greet(string name)
        {
            return $"Hello, {name}!";
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Examples")]
        public static int Square(int number)
        {
            return number * number;
        }

        // ==========================================
        // ASYNC NODES WITH DEPENDENCY INJECTION
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Examples")]
        public static async Task<string> FetchData(
            string url,
            IServiceProvider serviceProvider)  // ← Automatically injected, not shown in UI
        {
            var httpClient = serviceProvider.GetRequiredService<HttpClient>();
            return await httpClient.GetStringAsync(url);
        }

        // ==========================================
        // NODES WITH MULTIPLE OUTPUTS (BRANCHING)
        // ==========================================

        [NodeFlowPorts("positive", "negative", "zero")]
        [BlazorFlowNodeMethod(NodeType.Function, "Examples")]
        public static async Task CheckSign(int number, NodeContext context)
        {
            if (number > 0)
                await context.ExecutePortAsync("positive");
            else if (number < 0)
                await context.ExecutePortAsync("negative");
            else
                await context.ExecutePortAsync("zero");
        }

        // ==========================================
        // NODES RETURNING COMPLEX OBJECTS
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Examples")]
        public static PersonInfo CreatePerson(string name, int age)
        {
            return new PersonInfo
            {
                Name = name,
                Age = age,
                CreatedAt = DateTime.UtcNow
            };
        }

        // ==========================================
        // NODES WITH INPUT FIELDS (UI INPUTS)
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Examples")]
        public static string FormatTemplate(
            [BlazorFlowInputField] string template,  // ← Shows as text input in node editor
            string value)
        {
            return template.Replace("{value}", value);
        }

        // ==========================================
        // EVENT NODES (WORKFLOW ENTRY POINTS)
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Event, "Examples")]
        public static void StartExample()
        {
            // This is an entry point that can trigger a workflow
        }

        // ==========================================
        // ERROR HANDLING EXAMPLE
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Examples")]
        public static decimal Divide(decimal numerator, decimal denominator)
        {
            if (denominator == 0)
                throw new DivideByZeroException("Cannot divide by zero");

            return numerator / denominator;
        }

        // ==========================================
        // COMPLEX BUSINESS LOGIC EXAMPLE
        // ==========================================

        [BlazorFlowNodeMethod(NodeType.Function, "Examples")]
        public static async Task<OrderResult> ProcessOrder(
            int orderId,
            decimal amount,
            IServiceProvider serviceProvider)
        {
            // Access injected services
            var orderService = serviceProvider.GetService<IOrderService>();

            if (orderService != null)
            {
                return await orderService.ProcessAsync(orderId, amount);
            }

            // Fallback if service not registered
            return new OrderResult
            {
                Success = false,
                Message = "Order service not configured"
            };
        }
    }

    // ==========================================
    // SUPPORTING CLASSES
    // ==========================================

    public class PersonInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrderResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public interface IOrderService
    {
        Task<OrderResult> ProcessAsync(int orderId, decimal amount);
    }
}
