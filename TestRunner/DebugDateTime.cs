using BlazorExecutionFlow.Drawflow.BaseNodes;
using System.Text.Json;
using System.Text.Json.Nodes;

public static class DebugDateTime
{
    public static async Task Run()
    {
        Console.WriteLine("=== Debugging JsonSetString Test ===\n");

        // First test the method directly
        Console.WriteLine("Direct method call:");
        var directResult = BaseNodeCollection.JsonSetString(new JsonObject(), "name", "John");
        Console.WriteLine($"Direct result: {directResult}");
        Console.WriteLine($"Direct result serialized: {JsonSerializer.Serialize(directResult)}");

        // Test what JsonSerializer.SerializeToNode does to JsonObject
        Console.WriteLine("\nSerializeToNode test:");
        var serializedNode = JsonSerializer.SerializeToNode(directResult);
        Console.WriteLine($"SerializeToNode result type: {serializedNode?.GetType().Name}");
        Console.WriteLine($"SerializeToNode result: {serializedNode}");
        Console.WriteLine($"Is JsonObject: {serializedNode is JsonObject}");

        Console.WriteLine("\n=== End Debug ===\n");

        await Task.CompletedTask;
    }
}
