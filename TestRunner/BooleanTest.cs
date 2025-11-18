using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Drawflow.BaseNodes;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models.NodeV2;

namespace TestRunner
{
    public static class BooleanTest
    {
        public static async Task Run()
        {
            Console.WriteLine("=== Boolean Handling Test ===\n");

            // Test 1: Serialize a boolean to JsonNode
            Console.WriteLine("[Test 1] Boolean Serialization");
            var boolValue = true;
            var serialized = JsonSerializer.SerializeToNode(boolValue);
            Console.WriteLine($"  Serialized true: {serialized}");
            Console.WriteLine($"  Type: {serialized?.GetType().Name}");
            Console.WriteLine($"  ValueKind: {serialized?.GetValueKind()}");
            Console.WriteLine();

            // Test 2: Store boolean in a JsonObject like a node output
            Console.WriteLine("[Test 2] Boolean in Node Output");
            var output = new JsonObject();
            output["result"] = serialized;
            Console.WriteLine($"  Output: {output.ToJsonString()}");
            Console.WriteLine();

            // Test 3: Retrieve and deserialize the boolean
            Console.WriteLine("[Test 3] Boolean Retrieval");
            var retrieved = output["result"];
            Console.WriteLine($"  Retrieved: {retrieved}");
            Console.WriteLine($"  Type: {retrieved?.GetType().Name}");
            Console.WriteLine($"  ValueKind: {retrieved?.GetValueKind()}");

            try
            {
                var coerced = retrieved.CoerceToType(typeof(bool));
                Console.WriteLine($"  Coerced to bool: {coerced}");
                Console.WriteLine($"  Coerced type: {coerced?.GetType().Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR coercing to bool: {ex.Message}");
            }
            Console.WriteLine();

            // Test 4: Simulate a comparison node output -> If node input
            Console.WriteLine("[Test 4] Comparison Node -> If Node Simulation");

            // Create a simple comparison node
            var equalMethod = typeof(BaseNodeCollection).GetMethod("Equal",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(int), typeof(int) },
                null)!;

            var comparisonNode = new Node
            {
                BackingMethod = equalMethod,
                Id = "comparison",
                Section = "test",
                DrawflowNodeId = "test-comparison"
            };

            // Set up the output mapping (result -> result)
            comparisonNode.MethodOutputToNodeOutputMap = new List<PathMapEntry>
            {
                new PathMapEntry { From = "result", To = "result" }
            };

            // Create input for the comparison (5 == 5)
            var comparisonInput = new JsonObject
            {
                ["input"] = new JsonObject
                {
                    ["a"] = 5,
                    ["b"] = 5
                }
            };
            comparisonNode.Input = comparisonInput;

            // Execute the comparison node
            var filledParams = comparisonNode.GetMethodParametersFromInputResult(comparisonInput);
            var comparisonResult = await comparisonNode.InvokeBackingMethodPublic(filledParams);

            Console.WriteLine($"  Comparison Result: {comparisonResult.ToJsonString(new JsonSerializerOptions { WriteIndented = true })}");
            Console.WriteLine();

            // Now try to read that boolean output
            Console.WriteLine("[Test 5] Reading Boolean from Comparison Output");
            var boolOutput = comparisonResult.GetByPath("output.result");
            Console.WriteLine($"  Boolean output: {boolOutput}");
            Console.WriteLine($"  Type: {boolOutput?.GetType().Name}");
            Console.WriteLine($"  ValueKind: {boolOutput?.GetValueKind()}");

            if (boolOutput != null)
            {
                try
                {
                    var coercedBool = boolOutput.CoerceToType(typeof(bool));
                    Console.WriteLine($"  Coerced to bool: {coercedBool}");
                    Console.WriteLine($"  SUCCESS: Can read boolean from comparison output");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ERROR: Cannot coerce to bool - {ex.Message}");
                }
            }
            Console.WriteLine();

            // Test 6: Test with ToPlainObject (used by Scriban templates)
            Console.WriteLine("[Test 6] ToPlainObject Conversion (Scriban)");
            var plainObject = comparisonResult.ToPlainObject();
            Console.WriteLine($"  Plain object: {JsonSerializer.Serialize(plainObject, new JsonSerializerOptions { WriteIndented = true })}");

            // Check if the boolean is still a bool
            if (plainObject is Dictionary<string, object?> dict &&
                dict.TryGetValue("output", out var outputObj) &&
                outputObj is Dictionary<string, object?> outputDict &&
                outputDict.TryGetValue("result", out var resultObj))
            {
                Console.WriteLine($"  Result value: {resultObj}");
                Console.WriteLine($"  Result type: {resultObj?.GetType().Name}");
                Console.WriteLine($"  Is bool: {resultObj is bool}");
            }
            Console.WriteLine();

            Console.WriteLine("=== End Boolean Test ===\n");
            await Task.CompletedTask;
        }
    }

    // Extension to access private InvokeBackingMethod for testing
    public static class NodeTestExtensions
    {
        public static async Task<JsonObject> InvokeBackingMethodPublic(this Node node, object[] parameters)
        {
            var method = typeof(Node).GetMethod("InvokeBackingMethod",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == null)
                throw new InvalidOperationException("Could not find InvokeBackingMethod");

            var result = method.Invoke(node, new object[] { parameters });

            if (result is Task<JsonObject> task)
                return await task;

            throw new InvalidOperationException("InvokeBackingMethod did not return Task<JsonObject>");
        }

        public static object[] GetMethodParametersFromInputResult(this Node node, JsonObject input)
        {
            var method = typeof(Node).GetMethod("GetMethodParametersFromInputResult",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == null)
                throw new InvalidOperationException("Could not find GetMethodParametersFromInputResult");

            var result = method.Invoke(node, new object[] { input });
            return (object[])result!;
        }
    }
}
