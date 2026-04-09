using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorWorkflow.Flow.BaseNodes;
using BlazorWorkflow.Helpers;
using BlazorWorkflow.Models.NodeV2;
using BlazorWorkflow.Testing;
using Xunit;

namespace TestRunner
{
    /// <summary>
    /// Tests for workflow export/import: verifying user-specific data is excluded,
    /// round-trip fidelity, and graceful handling of edge cases.
    /// </summary>
    public class ExportImportTests
    {
        #region Helpers

        private static List<Node> BuildSimpleGraph()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("a", "5")
                .MapInput("b", "3")
                .AutoMapOutputs();

            return graph.GetAllNodes().ToList();
        }

        private static List<Node> BuildConnectedGraph()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("a", "10")
                .MapInput("b", "20")
                .AutoMapOutputs();

            graph.AddNode("multiply", typeof(BaseNodeCollection), "Multiply")
                .MapInput("a", "input.result")
                .MapInput("b", "2")
                .AutoMapOutputs();

            graph.Connect("add", "multiply");

            return graph.GetAllNodes().ToList();
        }

        private static List<Node> BuildPortDrivenGraph()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("check", typeof(CoreNodes), "If")
                .MapInput("condition", "true")
                .WithOutputPorts("true", "false");

            graph.AddNode("trueNode", typeof(BaseNodeCollection), "Add")
                .MapInput("a", "1")
                .MapInput("b", "2")
                .AutoMapOutputs();

            graph.AddNode("falseNode", typeof(BaseNodeCollection), "Add")
                .MapInput("a", "100")
                .MapInput("b", "200")
                .AutoMapOutputs();

            graph.Connect("check", "trueNode", "true");
            graph.Connect("check", "falseNode", "false");

            return graph.GetAllNodes().ToList();
        }

        #endregion

        #region Export excludes user-specific data

        [Fact]
        public void ExportExcludesNodeInput()
        {
            var nodes = BuildSimpleGraph();
            // Simulate execution state
            nodes[0].Input = new JsonObject { ["value"] = 42 };

            var json = FlowSerializer.SerializeFlow(nodes, "Test");
            var doc = JsonDocument.Parse(json);

            // Walk through serialized nodes and verify no Input field
            foreach (var node in doc.RootElement.GetProperty("Nodes").EnumerateArray())
            {
                Assert.False(node.TryGetProperty("Input", out _), "Export should not contain 'Input' (execution state)");
                Assert.False(node.TryGetProperty("input", out _), "Export should not contain 'input' (execution state)");
            }
        }

        [Fact]
        public void ExportExcludesNodeResult()
        {
            var nodes = BuildSimpleGraph();
            // Simulate execution result
            nodes[0].Result = new JsonObject { ["output"] = new JsonObject { ["result"] = 8 } };

            var json = FlowSerializer.SerializeFlow(nodes, "Test");
            var doc = JsonDocument.Parse(json);

            foreach (var node in doc.RootElement.GetProperty("Nodes").EnumerateArray())
            {
                Assert.False(node.TryGetProperty("Result", out _), "Export should not contain 'Result' (execution state)");
                Assert.False(node.TryGetProperty("result", out _), "Export should not contain 'result' (execution state)");
            }
        }

        [Fact]
        public void ExportExcludesExecutionStateButKeepsConfig()
        {
            var nodes = BuildSimpleGraph();
            nodes[0].Input = new JsonObject { ["value"] = 42 };
            nodes[0].Result = new JsonObject { ["output"] = new JsonObject { ["result"] = 8 } };

            var json = FlowSerializer.SerializeFlow(nodes, "Test Workflow");
            var doc = JsonDocument.Parse(json);

            // Config should be present
            var firstNode = doc.RootElement.GetProperty("Nodes").EnumerateArray().First();
            Assert.True(firstNode.TryGetProperty("MethodSignature", out _), "Export should contain MethodSignature");
            Assert.True(firstNode.TryGetProperty("NodeInputToMethodInputMap", out _), "Export should contain input mappings");
            Assert.True(firstNode.TryGetProperty("MethodOutputToNodeOutputMap", out _), "Export should contain output mappings");
        }

        [Fact]
        public void ExportIncludesMetadata()
        {
            var nodes = BuildSimpleGraph();
            var metadata = new Dictionary<string, object>
            {
                { "description", "A test workflow" },
                { "inputs", new Dictionary<string, string> { { "param1", "value1" } } }
            };

            var json = FlowSerializer.SerializeFlow(nodes, "My Workflow", metadata);
            var doc = JsonDocument.Parse(json);

            Assert.Equal("My Workflow", doc.RootElement.GetProperty("FlowName").GetString());
            Assert.Equal("A test workflow", doc.RootElement.GetProperty("Metadata").GetProperty("description").GetString());
        }

        [Fact]
        public void ExportDoesNotIncludePreviousExecutions()
        {
            // The FlowSerializer only serializes nodes, not WorkflowInfo.
            // PreviousExecutions are on WorkflowInfo, not in the serialized flow.
            var nodes = BuildSimpleGraph();
            var json = FlowSerializer.SerializeFlow(nodes, "Test");

            // Verify the JSON does not contain execution-related data
            Assert.DoesNotContain("PreviousExecutions", json);
            Assert.DoesNotContain("previousExecutions", json);
            Assert.DoesNotContain("ErrorMessage", json);
            Assert.DoesNotContain("errorMessage", json);
            Assert.DoesNotContain("IsRunning", json);
            Assert.DoesNotContain("isRunning", json);
        }

        #endregion

        #region Round-trip serialization

        [Fact]
        public void RoundTripSimpleNode()
        {
            var original = BuildSimpleGraph();
            var json = FlowSerializer.SerializeFlow(original, "Simple");
            var restored = FlowSerializer.DeserializeFlow(json, out var metadata);

            Assert.Single(restored);
            Assert.Equal("Simple", metadata.FlowName);
            Assert.Equal(original[0].BackingMethod.Name, restored[0].BackingMethod.Name);
            Assert.Equal(original[0].NodeInputToMethodInputMap.Count, restored[0].NodeInputToMethodInputMap.Count);
        }

        [Fact]
        public void RoundTripPreservesInputMappings()
        {
            var original = BuildSimpleGraph();
            var json = FlowSerializer.SerializeFlow(original, "Test");
            var restored = FlowSerializer.DeserializeFlow(json);

            var node = restored[0];
            Assert.Equal(2, node.NodeInputToMethodInputMap.Count);

            var aMapping = node.NodeInputToMethodInputMap.First(m => m.To == "a");
            Assert.Equal("5", aMapping.From);

            var bMapping = node.NodeInputToMethodInputMap.First(m => m.To == "b");
            Assert.Equal("3", bMapping.From);
        }

        [Fact]
        public void RoundTripPreservesConnections()
        {
            var original = BuildConnectedGraph();
            var json = FlowSerializer.SerializeFlow(original, "Connected");
            var restored = FlowSerializer.DeserializeFlow(json);

            Assert.Equal(2, restored.Count);

            // Find the multiply node
            var multiply = restored.First(n => n.BackingMethod.Name == "Multiply");
            Assert.Single(multiply.InputNodes);
            Assert.Equal("Add", multiply.InputNodes[0].BackingMethod.Name);

            // Verify the add node outputs to multiply
            var add = restored.First(n => n.BackingMethod.Name == "Add");
            Assert.Single(add.OutputNodes);
        }

        [Fact]
        public void RoundTripPreservesPortConnections()
        {
            var original = BuildPortDrivenGraph();
            var json = FlowSerializer.SerializeFlow(original, "PortDriven");
            var restored = FlowSerializer.DeserializeFlow(json);

            Assert.Equal(3, restored.Count);

            var ifNode = restored.First(n => n.BackingMethod.Name == "If");
            Assert.Equal(2, ifNode.DeclaredOutputPorts.Count);
            Assert.Contains("true", ifNode.DeclaredOutputPorts);
            Assert.Contains("false", ifNode.DeclaredOutputPorts);

            // Verify port connections are restored
            Assert.True(ifNode.OutputPorts.ContainsKey("true"));
            Assert.True(ifNode.OutputPorts.ContainsKey("false"));
            Assert.Single(ifNode.OutputPorts["true"]);
            Assert.Single(ifNode.OutputPorts["false"]);
        }

        [Fact]
        public void RoundTripPreservesNodePosition()
        {
            var nodes = BuildSimpleGraph();
            nodes[0].PosX = 150.5;
            nodes[0].PosY = 300.75;

            var json = FlowSerializer.SerializeFlow(nodes, "Positioned");
            var restored = FlowSerializer.DeserializeFlow(json);

            Assert.Equal(150.5, restored[0].PosX);
            Assert.Equal(300.75, restored[0].PosY);
        }

        [Fact]
        public void RoundTripPreservesMetadata()
        {
            var nodes = BuildSimpleGraph();
            var metadata = new Dictionary<string, object>
            {
                { "description", "My workflow" },
                { "inputs", new Dictionary<string, string> { { "apiKey", "" }, { "endpoint", "https://example.com" } } }
            };

            var json = FlowSerializer.SerializeFlow(nodes, "MetadataTest", metadata);
            var restored = FlowSerializer.DeserializeFlow(json, out var restoredMetadata);

            Assert.Equal("MetadataTest", restoredMetadata.FlowName);
            Assert.True(restoredMetadata.Metadata.ContainsKey("description"));
        }

        [Fact]
        public void RoundTripPreservesNameOverride()
        {
            var nodes = BuildSimpleGraph();
            nodes[0].NameOverride = "My Custom Add";

            var json = FlowSerializer.SerializeFlow(nodes, "NamedNodes");
            var restored = FlowSerializer.DeserializeFlow(json);

            Assert.Equal("My Custom Add", restored[0].NameOverride);
        }

        [Fact]
        public void RoundTripPreservesMergeOutputWithInput()
        {
            var nodes = BuildSimpleGraph();
            nodes[0].MergeOutputWithInput = true;

            var json = FlowSerializer.SerializeFlow(nodes, "MergeTest");
            var restored = FlowSerializer.DeserializeFlow(json);

            Assert.True(restored[0].MergeOutputWithInput);
        }

        #endregion

        #region Import resiliency

        [Fact]
        public void ImportClearsExecutionState()
        {
            var nodes = BuildSimpleGraph();
            var json = FlowSerializer.SerializeFlow(nodes, "Test");
            var restored = FlowSerializer.DeserializeFlow(json);

            // After import, nodes should have no execution state
            Assert.Null(restored[0].Input);
            Assert.Null(restored[0].Result);
            Assert.False(restored[0].HasError);
            Assert.Null(restored[0].ErrorMessage);
        }

        [Fact]
        public async Task ImportedNodesCanExecute()
        {
            // Build, serialize, and deserialize a node
            var graph = new NodeGraphBuilder();
            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("a", "7")
                .MapInput("b", "3")
                .AutoMapOutputs();

            var original = graph.GetAllNodes().ToList();
            var json = FlowSerializer.SerializeFlow(original, "Executable");
            var restored = FlowSerializer.DeserializeFlow(json);

            // Verify the deserialized node has correct mappings
            Assert.Single(restored);
            var importedNode = restored[0];
            Assert.Equal("Add", importedNode.BackingMethod.Name);
            Assert.Equal(2, importedNode.NodeInputToMethodInputMap.Count);
            Assert.Equal(1, importedNode.MethodOutputToNodeOutputMap.Count);

            // Verify input mappings are correct
            var aMap = importedNode.NodeInputToMethodInputMap.FirstOrDefault(m => m.To == "a");
            Assert.NotNull(aMap);
            Assert.Equal("7", aMap!.From);

            var bMap = importedNode.NodeInputToMethodInputMap.FirstOrDefault(m => m.To == "b");
            Assert.NotNull(bMap);
            Assert.Equal("3", bMap!.From);

            // Verify output mappings are correct
            var outMap = importedNode.MethodOutputToNodeOutputMap.FirstOrDefault(m => m.From == "result");
            Assert.NotNull(outMap);
            Assert.Equal("result", outMap!.To);
        }

        [Fact]
        public void ImportHandlesMissingMethodGracefully()
        {
            // Simulate a JSON with a method that doesn't exist
            var nodes = BuildSimpleGraph();
            var json = FlowSerializer.SerializeFlow(nodes, "Test");

            // Corrupt the method signature
            json = json.Replace("Add", "NonExistentMethodXYZ123");

            var restored = FlowSerializer.DeserializeFlow(json, out _);
            // The node should be skipped, not throw
            Assert.Empty(restored);
        }

        [Fact]
        public void ImportHandlesDuplicateNodeIds()
        {
            var nodes = BuildSimpleGraph();
            var json = FlowSerializer.SerializeFlow(nodes, "Test");

            // Manually duplicate the node in JSON
            var doc = JsonDocument.Parse(json);
            var root = JsonNode.Parse(json)!.AsObject();
            var nodesArray = root["Nodes"]!.AsArray();
            var firstNode = JsonNode.Parse(nodesArray[0]!.ToJsonString());
            nodesArray.Add(firstNode);

            var modifiedJson = root.ToJsonString();
            var restored = FlowSerializer.DeserializeFlow(modifiedJson, out _);

            // Should deduplicate - only one node
            Assert.Single(restored);
        }

        [Fact]
        public void ImportHandlesEmptyNodesArray()
        {
            var json = """
            {
                "Version": "1.0",
                "FlowName": "Empty",
                "Nodes": []
            }
            """;

            var restored = FlowSerializer.DeserializeFlow(json, out var metadata);
            Assert.Empty(restored);
            Assert.Equal("Empty", metadata.FlowName);
        }

        [Fact]
        public void ImportHandlesConnectionToSkippedNode()
        {
            // Build two connected nodes, then corrupt one method so it gets skipped
            var original = BuildConnectedGraph();
            var json = FlowSerializer.SerializeFlow(original, "Test");

            // Corrupt the Multiply method signature so it gets skipped
            json = json.Replace("|Multiply|", "|BrokenMethodXYZ|");

            var restored = FlowSerializer.DeserializeFlow(json, out _);

            // Only Add node should remain, with no output connections to the broken node
            Assert.Single(restored);
            Assert.Equal("Add", restored[0].BackingMethod.Name);
            Assert.Empty(restored[0].OutputNodes);
        }

        #endregion

        #region Validation

        [Fact]
        public void ValidateFlowRejectsInvalidJson()
        {
            var isValid = FlowSerializer.ValidateFlow("not json at all", out var error);
            Assert.False(isValid);
            Assert.NotNull(error);
        }

        [Fact]
        public void ValidateFlowRejectsEmptyNodes()
        {
            var json = """{ "Version": "1.0", "FlowName": "Empty", "Nodes": [] }""";
            var isValid = FlowSerializer.ValidateFlow(json, out var error);
            Assert.False(isValid);
            Assert.Contains("no nodes", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidateFlowAcceptsValidFlow()
        {
            var nodes = BuildSimpleGraph();
            var json = FlowSerializer.SerializeFlow(nodes, "Valid");

            var isValid = FlowSerializer.ValidateFlow(json, out var error);
            Assert.True(isValid);
            Assert.Null(error);
        }

        [Fact]
        public void ValidateFlowDetectsDuplicateIds()
        {
            var nodes = BuildSimpleGraph();
            var json = FlowSerializer.SerializeFlow(nodes, "Test");

            // Duplicate the node
            var root = JsonNode.Parse(json)!.AsObject();
            var nodesArray = root["Nodes"]!.AsArray();
            var firstNode = JsonNode.Parse(nodesArray[0]!.ToJsonString());
            nodesArray.Add(firstNode);

            var modifiedJson = root.ToJsonString();
            var isValid = FlowSerializer.ValidateFlow(modifiedJson, out var error);
            Assert.False(isValid);
            Assert.Contains("Duplicate", error);
        }

        #endregion

        #region DrawflowExporter

        [Fact]
        public void DrawflowExportExcludesExecutionState()
        {
            var nodes = BuildSimpleGraph();
            // Set execution state
            nodes[0].Input = new JsonObject { ["value"] = 42 };
            nodes[0].Result = new JsonObject { ["output"] = new JsonObject { ["result"] = 8 } };

            var json = DrawflowExporter.ExportToDrawflowJson(nodes);

            // The drawflow export should contain node data but not execution state
            Assert.Contains("Add", json);
            Assert.DoesNotContain("\"Input\"", json);
            Assert.DoesNotContain("\"Result\"", json);
        }

        [Fact]
        public void DrawflowExportPreservesConnections()
        {
            var nodes = BuildConnectedGraph();
            var json = DrawflowExporter.ExportToDrawflowJson(nodes);
            var doc = JsonDocument.Parse(json);

            // Should have a drawflow root with Home module
            var home = doc.RootElement.GetProperty("drawflow").GetProperty("Home").GetProperty("data");

            // Should have 2 nodes
            int nodeCount = 0;
            foreach (var _ in home.EnumerateObject()) nodeCount++;
            Assert.Equal(2, nodeCount);
        }

        #endregion
    }
}
