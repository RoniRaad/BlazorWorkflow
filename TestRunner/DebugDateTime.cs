using DrawflowWrapper.Testing;
using DrawflowWrapper.Drawflow.BaseNodes;
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

        // Now test through the graph
        Console.WriteLine("\nThrough node graph:");
        var graph = new NodeGraphBuilder();
        graph.AddNode("set", typeof(BaseNodeCollection), "JsonSetString")
            .MapInput("obj", "{}")
            .MapInput("path", "\"name\"")
            .MapInput("value", "\"John\"")
            .AutoMapOutputs();

        var result = await graph.ExecuteAsync("set");
        var nodeResult = result.GetNodeResult("set");

        Console.WriteLine($"Full Result JSON:\n{JsonSerializer.Serialize(nodeResult, new JsonSerializerOptions { WriteIndented = true })}\n");

        try
        {
            var output = result.GetOutput<JsonObject>("set", "result");
            Console.WriteLine($"Output JsonObject: {output}");

            if (output != null)
            {
                Console.WriteLine($"Output has 'name' property: {output.ContainsKey("name")}");
                if (output.ContainsKey("name"))
                {
                    var nameValue = output["name"]?.GetValue<string>();
                    Console.WriteLine($"Name value: {nameValue}");
                    Console.WriteLine($"Test expects: 'John'");
                }
            }
            else
            {
                Console.WriteLine("Output is null");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\n=== End Debug ===\n");
    }
}
