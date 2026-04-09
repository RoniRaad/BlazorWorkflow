using BlazorWorkflow.Flow.BaseNodes;
using BlazorWorkflow.Testing;
using Xunit;

namespace TestRunner
{
    /// <summary>
    /// Tests for SharedExecutionContext: cross-node state access,
    /// workflow parameters, and output propagation.
    /// </summary>
    public class SharedContextTests
    {
        #region Cross-Node State Access

        [Fact]
        public async Task TestNodeResultStoredInSharedContext()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "20")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("add");

            // Node result should be accessible via shared context path
            var sharedValue = result.GetSharedContextValue($"nodes.byId.node_{graph.GetNode("add").DrawflowNodeId}.result");
            Assert.NotNull(sharedValue);
            Assert.Equal(30, sharedValue!.GetValue<int>());
        }

        [Fact]
        public async Task TestChainedNodesShareContext()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("first", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "5")
                .MapInput("input2", "3")
                .AutoMapOutputs();

            graph.AddNode("second", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "2")
                .AutoMapOutputs();

            graph.Connect("first", "second");

            var result = await graph.ExecuteAsync("first");

            // Both nodes should have results in shared context
            var firstId = graph.GetNode("first").DrawflowNodeId;
            var secondId = graph.GetNode("second").DrawflowNodeId;

            Assert.NotNull(result.GetSharedContextValue($"nodes.byId.node_{firstId}.result"));
            Assert.NotNull(result.GetSharedContextValue($"nodes.byId.node_{secondId}.result"));
        }

        #endregion

        #region Error Handling

        [Fact]
        public async Task TestNodeErrorSetsHasError()
        {
            var graph = new NodeGraphBuilder();

            // Division by zero should cause an error
            graph.AddNode("div", typeof(BaseNodeCollection), "Divide")
                .MapInput("input1", "10")
                .MapInput("input2", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("div");

            Assert.True(result.HasError("div"));
            Assert.NotNull(result.GetErrorMessage("div"));
        }

        [Fact]
        public async Task TestErrorDoesNotCrashDownstream()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("div", typeof(BaseNodeCollection), "Divide")
                .MapInput("input1", "10")
                .MapInput("input2", "0")
                .AutoMapOutputs();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "5")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.Connect("div", "add");

            // Should not throw — downstream executes with error result as input
            var result = await graph.ExecuteAsync("div");

            Assert.True(result.HasError("div"));
            // Downstream should still execute (may get default values)
            Assert.NotNull(result.GetNodeResult("add"));
        }

        [Fact]
        public async Task TestDivisionByZeroDoubleError()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("div", typeof(BaseNodeCollection), "DivideD")
                .MapInput("numerator", "10")
                .MapInput("denominator", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("div");

            Assert.True(result.HasError("div"));
        }

        #endregion

        #region Multiple Input Confluence

        [Fact]
        public async Task TestTwoInputNodesMergeResults()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("a", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.AddNode("b", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "100")
                .MapInput("input2", "50")
                .AutoMapOutputs();

            // Both A and B feed into C
            // C gets merged input from both
            graph.AddNode("c", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "3")
                .MapInput("input2", "7")
                .AutoMapOutputs();

            graph.Connect("a", "c");
            graph.Connect("b", "c");

            var result = await graph.ExecuteAsync("a");

            // A and B should have their results
            Assert.Equal(15, result.GetOutput<int>("a", "result"));
            Assert.Equal(150, result.GetOutput<int>("b", "result"));

            // C should have executed (its inputs are literals, not dependent on A/B)
            Assert.Equal(10, result.GetOutput<int>("c", "result"));
        }

        #endregion

        #region Workflow Parameters

        [Fact]
        public async Task TestWorkflowParametersAccessible()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "1")
                .MapInput("input2", "2")
                .AutoMapOutputs();

            var parameters = new Dictionary<string, string>
            {
                ["myParam"] = "hello"
            };

            var result = await graph.ExecuteAsync("add", parameters: parameters);

            // Workflow parameters should be in shared context
            var paramValue = result.GetSharedContextValue("workflow.parameters.myParam");
            Assert.NotNull(paramValue);
            Assert.Equal("hello", paramValue!.GetValue<string>());
        }

        #endregion
    }
}
