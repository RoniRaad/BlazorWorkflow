using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models.NodeV2;
using Xunit;

namespace TestRunner
{
    public class BooleanTest
    {
        [Fact]
        public void TestBooleanSerialization()
        {
            // Test 1: Serialize a boolean to JsonNode
            var boolValue = true;
            var serialized = JsonSerializer.SerializeToNode(boolValue);

            Assert.NotNull(serialized);
            Assert.Equal(JsonValueKind.True, serialized.GetValueKind());
        }

        [Fact]
        public void TestBooleanInNodeOutput()
        {
            // Store boolean in a JsonObject like a node output
            var boolValue = true;
            var serialized = JsonSerializer.SerializeToNode(boolValue);
            var output = new JsonObject();
            output["result"] = serialized;

            Assert.NotNull(output["result"]);
        }

        [Fact]
        public void TestBooleanRetrieval()
        {
            // Retrieve and deserialize the boolean
            var boolValue = true;
            var serialized = JsonSerializer.SerializeToNode(boolValue);
            var output = new JsonObject();
            output["result"] = serialized;

            var retrieved = output["result"];
            Assert.NotNull(retrieved);

            var coerced = retrieved.CoerceToType(typeof(bool));
            Assert.IsType<bool>(coerced);
            Assert.True((bool)coerced);
        }

        [Fact]
        public async Task TestComparisonNodeOutput()
        {
            // Simulate a comparison node output -> If node input
            var equalMethod = typeof(CoreNodes).GetMethod("Equal",
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

            comparisonNode.MethodOutputToNodeOutputMap = new List<PathMapEntry>
            {
                new PathMapEntry { From = "result", To = "result" }
            };

            var comparisonInput = new JsonObject
            {
                ["input"] = new JsonObject
                {
                    ["a"] = 5,
                    ["b"] = 5
                }
            };
            comparisonNode.Input = comparisonInput;

            var filledParams = comparisonNode.GetMethodParametersFromInputResult(comparisonInput);
            var comparisonResult = await comparisonNode.InvokeBackingMethodPublic(filledParams);

            var boolOutput = comparisonResult.GetByPath("output.result");
            Assert.NotNull(boolOutput);

            var coercedBool = boolOutput.CoerceToType(typeof(bool));
            Assert.IsType<bool>(coercedBool);
            Assert.True((bool)coercedBool);
        }

        [Fact]
        public async Task TestToPlainObjectConversion()
        {
            // Test with ToPlainObject (used by Scriban templates)
            var equalMethod = typeof(CoreNodes).GetMethod("Equal",
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

            comparisonNode.MethodOutputToNodeOutputMap = new List<PathMapEntry>
            {
                new PathMapEntry { From = "result", To = "result" }
            };

            var comparisonInput = new JsonObject
            {
                ["input"] = new JsonObject
                {
                    ["a"] = 5,
                    ["b"] = 5
                }
            };
            comparisonNode.Input = comparisonInput;

            var filledParams = comparisonNode.GetMethodParametersFromInputResult(comparisonInput);
            var comparisonResult = await comparisonNode.InvokeBackingMethodPublic(filledParams);

            var plainObject = comparisonResult.ToPlainObject();
            Assert.NotNull(plainObject);

            // Check if the boolean is still a bool
            Assert.IsType<Dictionary<string, object?>>(plainObject);
            var dict = (Dictionary<string, object?>)plainObject;
            Assert.True(dict.TryGetValue("output", out var outputObj));
            Assert.IsType<Dictionary<string, object?>>(outputObj);
            var outputDict = (Dictionary<string, object?>)outputObj;
            Assert.True(outputDict.TryGetValue("result", out var resultObj));
            Assert.IsType<bool>(resultObj);
            Assert.True((bool)resultObj);
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
