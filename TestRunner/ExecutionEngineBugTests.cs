using System.Collections.Frozen;
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
    /// Regression tests for execution engine bugs that were found and fixed.
    /// Each test targets a specific bug to prevent regressions.
    /// </summary>
    public class ExecutionEngineBugTests
    {
        #region Bug 1: GetScribanObject mutates inputPayload

        [Fact]
        public async Task ScribanResolution_DoesNotMutateInputPayload()
        {
            // Bug: ScribanHelpers.GetScribanObject was calling inputPayload.Merge(SharedContext)
            // which permanently polluted the node's Input with shared context data.
            // After fix, the original inputPayload should remain untouched.

            var graph = new NodeGraphBuilder();

            // Use workflow parameters so SharedContext has data to merge
            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "{{ workflow.parameters.x }}")
                .MapInput("input2", "{{ workflow.parameters.y }}")
                .AutoMapOutputs();

            var parameters = new Dictionary<string, string>
            {
                ["x"] = "10",
                ["y"] = "20"
            };

            var result = await graph.ExecuteAsync("add", parameters: parameters);

            // The node should produce correct result
            Assert.Equal(30, result.GetOutput<int>("add", "result"));

            // The node's Input should NOT contain "workflow", "environment", "nodes" keys
            // that come from SharedContext — those should not have leaked in
            var node = graph.GetNode("add");
            Assert.NotNull(node.Input);
            Assert.False(node.Input!.ContainsKey("workflow"),
                "Input should not be polluted with SharedContext 'workflow' key");
            Assert.False(node.Input!.ContainsKey("environment"),
                "Input should not be polluted with SharedContext 'environment' key");
            Assert.False(node.Input!.ContainsKey("nodes"),
                "Input should not be polluted with SharedContext 'nodes' key");
        }

        [Fact]
        public async Task ScribanResolution_MultipleParams_DoNotCrossContaminate()
        {
            // Bug: When resolving param A, the merge polluted inputPayload.
            // Then param B saw the polluted payload. This could cause subtle data leaks.

            var graph = new NodeGraphBuilder();

            graph.AddNode("first", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "5")
                .MapInput("input2", "3")
                .AutoMapOutputs();

            graph.AddNode("second", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "{{ workflow.parameters.factor }}")
                .AutoMapOutputs();

            graph.Connect("first", "second");

            var parameters = new Dictionary<string, string> { ["factor"] = "4" };
            var result = await graph.ExecuteAsync("first", parameters: parameters);

            // first: 5 + 3 = 8, second: 8 * 4 = 32
            Assert.Equal(32, result.GetOutput<int>("second", "result"));

            // Verify second node's Input wasn't polluted
            var secondNode = graph.GetNode("second");
            Assert.NotNull(secondNode.Input);
            Assert.False(secondNode.Input!.ContainsKey("nodes"),
                "Second node's Input should not contain shared context 'nodes' key");
        }

        #endregion

        #region Bug 2: HandleExecutionException null Result

        [Fact]
        public async Task ErrorHandler_ReturnsEmptyObject_NotNull()
        {
            // Bug: catch block returned Result! which could NRE if HandleExecutionException
            // itself failed. After fix, returns Result ?? [].

            var graph = new NodeGraphBuilder();

            // Division by zero triggers the error handler
            graph.AddNode("div", typeof(BaseNodeCollection), "Divide")
                .MapInput("input1", "10")
                .MapInput("input2", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("div");

            // Should have error state but no crash
            Assert.True(result.HasError("div"));
            // Result should not be null — should be the error object
            Assert.NotNull(result.GetNodeResult("div"));
        }

        [Fact]
        public async Task ErrorNode_DownstreamStillExecutes()
        {
            // Verify that a node error doesn't produce a null that crashes downstream

            var graph = new NodeGraphBuilder();

            graph.AddNode("div", typeof(BaseNodeCollection), "Divide")
                .MapInput("input1", "10")
                .MapInput("input2", "0")
                .AutoMapOutputs();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "1")
                .MapInput("input2", "2")
                .AutoMapOutputs();

            graph.Connect("div", "add");

            // Should not throw
            var result = await graph.ExecuteAsync("div");

            Assert.True(result.HasError("div"));
            Assert.NotNull(result.GetNodeResult("add"));
        }

        #endregion

        #region Bug 3: ClearDownstreamResults infinite loop on cycles

        [Fact]
        public void ClearDownstreamResults_HandlesCycles_WithoutStackOverflow()
        {
            // Bug: ClearDownstreamResults had no visited set, so graph cycles
            // caused infinite recursion and StackOverflowException.

            // Build a cycle: A -> B -> C -> A
            var methodAdd = typeof(BaseNodeCollection).GetMethod("Add",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;

            var nodeA = new Node { BackingMethod = methodAdd, DrawflowNodeId = "a" };
            var nodeB = new Node { BackingMethod = methodAdd, DrawflowNodeId = "b" };
            var nodeC = new Node { BackingMethod = methodAdd, DrawflowNodeId = "c" };

            nodeA.AddOutputConnection(null, nodeB);
            nodeB.AddOutputConnection(null, nodeC);
            nodeC.AddOutputConnection(null, nodeA); // Cycle!

            // Give them all results
            nodeA.Result = new JsonObject { ["output"] = new JsonObject { ["result"] = 1 } };
            nodeB.Result = new JsonObject { ["output"] = new JsonObject { ["result"] = 2 } };
            nodeC.Result = new JsonObject { ["output"] = new JsonObject { ["result"] = 3 } };

            // This should NOT throw StackOverflowException
            nodeA.ClearDownstreamResults();

            // All downstream nodes should be cleared
            Assert.Null(nodeB.Result);
            Assert.Null(nodeC.Result);
            // nodeA itself is not cleared by ClearDownstreamResults (it clears its *downstream* nodes)
            // but the cycle means A is downstream of C, so it should be cleared too
            Assert.Null(nodeA.Result);
        }

        [Fact]
        public void ClearDownstreamResults_SelfLoop_DoesNotInfiniteLoop()
        {
            // Edge case: a node connected to itself
            var methodAdd = typeof(BaseNodeCollection).GetMethod("Add",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;

            var node = new Node { BackingMethod = methodAdd, DrawflowNodeId = "self" };
            node.AddOutputConnection(null, node); // Self-loop

            node.Result = new JsonObject { ["output"] = new JsonObject { ["result"] = 42 } };

            // Should not hang or throw
            node.ClearDownstreamResults();

            Assert.Null(node.Result);
        }

        [Fact]
        public void ClearDownstreamResults_DiamondGraph_ClearsAll()
        {
            // Diamond: A -> B, A -> C, B -> D, C -> D
            var methodAdd = typeof(BaseNodeCollection).GetMethod("Add",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;

            var nodeA = new Node { BackingMethod = methodAdd, DrawflowNodeId = "a" };
            var nodeB = new Node { BackingMethod = methodAdd, DrawflowNodeId = "b" };
            var nodeC = new Node { BackingMethod = methodAdd, DrawflowNodeId = "c" };
            var nodeD = new Node { BackingMethod = methodAdd, DrawflowNodeId = "d" };

            nodeA.AddOutputConnection(null, nodeB);
            nodeA.AddOutputConnection(null, nodeC);
            nodeB.AddOutputConnection(null, nodeD);
            nodeC.AddOutputConnection(null, nodeD);

            nodeB.Result = new JsonObject { ["v"] = 1 };
            nodeC.Result = new JsonObject { ["v"] = 2 };
            nodeD.Result = new JsonObject { ["v"] = 3 };

            nodeA.ClearDownstreamResults();

            Assert.Null(nodeB.Result);
            Assert.Null(nodeC.Result);
            Assert.Null(nodeD.Result);
        }

        #endregion

        #region Bug 6: ForEachJson/MapJson detach JsonNode from collection

        [Fact]
        public async Task ForEachJson_DoesNotCorruptSourceCollection()
        {
            // Bug: outputData["currentItem"] = item detached the JsonNode from the
            // source collection because JsonNode can only have one parent.
            // After fix, items are cloned before assignment.

            var graph = new NodeGraphBuilder();

            graph.AddNode("forEach", typeof(AdvancedIterationNodes), "ForEachJson")
                .MapInput("collection", "[{\"name\":\"a\"},{\"name\":\"b\"},{\"name\":\"c\"}]")
                .WithOutputPorts("item", "done")
                .AutoMapOutputs();

            // Connect a simple downstream node to the "item" port
            graph.AddNode("log", typeof(CoreNodes), "Log")
                .MapInput("message", "input.currentItem");

            graph.Connect("forEach", "log", "item");

            var result = await graph.ExecuteAsync("forEach");

            // Verify all 3 items were processed
            Assert.Equal(3, result.GetOutput<int>("forEach", "ItemCount"));
        }

        [Fact]
        public async Task MapJson_DoesNotCorruptSourceCollection()
        {
            // Same bug but in MapJson — item assignment detaches from collection.

            var graph = new NodeGraphBuilder();

            graph.AddNode("map", typeof(AdvancedIterationNodes), "MapJson")
                .MapInput("collection", "[{\"v\":1},{\"v\":2},{\"v\":3}]")
                .WithOutputPorts("transform", "done")
                .AutoMapOutputs();

            // Connect a transform node
            graph.AddNode("double", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.v")
                .MapInput("input2", "2")
                .AutoMapOutputs();

            graph.Connect("map", "double", "transform");

            // Should not throw or produce incorrect results from collection corruption
            var result = await graph.ExecuteAsync("map");

            var transformed = result.GetOutput<JsonNode>("map", "TransformedItems");
            Assert.NotNull(transformed);
        }

        #endregion

        #region Bug 7: SharedContext getter creates disconnected objects

        [Fact]
        public void SharedContext_ParametersAvailable_OnMultipleAccesses()
        {
            // Bug: SharedContext getter created a new workflowObj each call but TryAdd
            // only inserted it the first time. On subsequent calls, parameters were added
            // to a discarded object. After fix, initialization happens only once.

            var context = new GraphExecutionContext
            {
                Parameters = new Dictionary<string, string>
                {
                    ["key1"] = "value1",
                    ["key2"] = "value2"
                }.ToFrozenDictionary()
            };

            // Access SharedContext multiple times
            var first = context.SharedContext;
            var second = context.SharedContext;

            // Both should return the same object
            Assert.Same(first, second);

            // Parameters should be accessible on every access
            var params1 = first.GetByPath("workflow.parameters.key1");
            Assert.NotNull(params1);
            Assert.Equal("value1", params1!.GetValue<string>());

            var params2 = second.GetByPath("workflow.parameters.key2");
            Assert.NotNull(params2);
            Assert.Equal("value2", params2!.GetValue<string>());
        }

        [Fact]
        public void SharedContext_EnvironmentVariables_PersistAcrossAccesses()
        {
            var context = new GraphExecutionContext
            {
                EnvironmentVariables = new Dictionary<string, string>
                {
                    ["API_KEY"] = "secret123"
                }.ToFrozenDictionary()
            };

            // Access multiple times
            _ = context.SharedContext;
            _ = context.SharedContext;
            var ctx = context.SharedContext;

            var envVar = ctx.GetByPath("environment.API_KEY");
            Assert.NotNull(envVar);
            Assert.Equal("secret123", envVar!.GetValue<string>());
        }

        [Fact]
        public void SharedContext_NodeDataSurvivesAlongsideInitializedData()
        {
            // Ensure that after SharedContext initializes workflow/environment,
            // node execution data written later is not lost

            var context = new GraphExecutionContext
            {
                Parameters = new Dictionary<string, string>
                {
                    ["p"] = "val"
                }.ToFrozenDictionary()
            };

            // Trigger initialization
            var ctx = context.SharedContext;

            // Simulate a node writing to shared context (like MapOutputValues does)
            ctx.SetByPath("nodes.byId.node_1.result", 42);

            // Both should coexist
            Assert.Equal("val", ctx.GetByPath("workflow.parameters.p")!.GetValue<string>());
            Assert.Equal(42, ctx.GetByPath("nodes.byId.node_1.result")!.GetValue<int>());
        }

        [Fact]
        public async Task SharedContext_ParametersAccessible_DuringExecution()
        {
            // End-to-end: verify Scriban templates can access workflow parameters
            // after the SharedContext getter fix

            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "{{ workflow.parameters.a }}")
                .MapInput("input2", "{{ workflow.parameters.b }}")
                .AutoMapOutputs();

            var parameters = new Dictionary<string, string>
            {
                ["a"] = "7",
                ["b"] = "3"
            };

            var result = await graph.ExecuteAsync("add", parameters: parameters);

            Assert.Equal(10, result.GetOutput<int>("add", "result"));
        }

        #endregion
    }
}
