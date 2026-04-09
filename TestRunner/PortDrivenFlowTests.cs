using BlazorWorkflow.Flow.BaseNodes;
using BlazorWorkflow.Testing;
using Xunit;

namespace TestRunner
{
    /// <summary>
    /// Tests for port-driven flow control: If branching, For/Repeat loops,
    /// and ForEach/Map iteration nodes with actual downstream execution.
    /// These tests verify that port-driven nodes correctly route execution
    /// to the right downstream targets.
    /// </summary>
    public class PortDrivenFlowTests
    {
        #region If Branching

        [Fact]
        public async Task TestIfTrueBranch()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("if", typeof(CoreNodes), "If")
                .MapInput("condition", "true")
                .WithOutputPorts("true", "false");

            graph.AddNode("trueNode", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "20")
                .AutoMapOutputs();

            graph.AddNode("falseNode", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "100")
                .MapInput("input2", "200")
                .AutoMapOutputs();

            graph.Connect("if", "trueNode", "true");
            graph.Connect("if", "falseNode", "false");

            var result = await graph.ExecuteAsync("if");

            // True branch should execute
            Assert.Equal(30, result.GetOutput<int>("trueNode", "result"));
            // False branch should NOT execute
            Assert.Null(result.GetNodeResult("falseNode"));
        }

        [Fact]
        public async Task TestIfFalseBranch()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("if", typeof(CoreNodes), "If")
                .MapInput("condition", "false")
                .WithOutputPorts("true", "false");

            graph.AddNode("trueNode", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "20")
                .AutoMapOutputs();

            graph.AddNode("falseNode", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "100")
                .MapInput("input2", "200")
                .AutoMapOutputs();

            graph.Connect("if", "trueNode", "true");
            graph.Connect("if", "falseNode", "false");

            var result = await graph.ExecuteAsync("if");

            // True branch should NOT execute
            Assert.Null(result.GetNodeResult("trueNode"));
            // False branch should execute
            Assert.Equal(300, result.GetOutput<int>("falseNode", "result"));
        }

        [Fact]
        public async Task TestIfWithChainedDownstream()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("if", typeof(CoreNodes), "If")
                .MapInput("condition", "true")
                .WithOutputPorts("true", "false");

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "5")
                .MapInput("input2", "3")
                .AutoMapOutputs();

            graph.AddNode("multiply", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "2")
                .AutoMapOutputs();

            graph.Connect("if", "add", "true");
            graph.Connect("add", "multiply");

            var result = await graph.ExecuteAsync("if");

            Assert.Equal(8, result.GetOutput<int>("add", "result"));
            Assert.Equal(16, result.GetOutput<int>("multiply", "result"));
        }

        #endregion

        #region For Loop

        [Fact]
        public async Task TestForLoopExecutesDonePort()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("for", typeof(CoreNodes), "For")
                .MapInput("start", "0")
                .MapInput("end", "3")
                .WithOutputPorts("loop", "done");

            graph.AddNode("afterLoop", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.Connect("for", "afterLoop", "done");

            var result = await graph.ExecuteAsync("for");

            // The "done" port should execute after the loop completes
            Assert.Equal(15, result.GetOutput<int>("afterLoop", "result"));
        }

        [Fact]
        public async Task TestRepeatLoop()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("repeat", typeof(CoreNodes), "Repeat")
                .MapInput("count", "5")
                .WithOutputPorts("loop", "done");

            graph.AddNode("afterRepeat", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "1")
                .MapInput("input2", "1")
                .AutoMapOutputs();

            graph.Connect("repeat", "afterRepeat", "done");

            var result = await graph.ExecuteAsync("repeat");

            Assert.Equal(2, result.GetOutput<int>("afterRepeat", "result"));
        }

        #endregion

        #region ForEach Iteration

        [Fact]
        public async Task TestForEachStringExecutesDonePort()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("forEach", typeof(AdvancedIterationNodes), "ForEachString")
                .MapInput("collection", "[\"a\",\"b\",\"c\"]")
                .WithOutputPorts("item", "done")
                .AutoMapOutputs();

            graph.AddNode("afterLoop", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "99")
                .MapInput("input2", "1")
                .AutoMapOutputs();

            graph.Connect("forEach", "afterLoop", "done");

            var result = await graph.ExecuteAsync("forEach");

            // Done port should execute after all iterations
            Assert.Equal(100, result.GetOutput<int>("afterLoop", "result"));
            // ForEach should report item count
            Assert.Equal(3, result.GetOutput<int>("forEach", "ItemCount"));
        }

        [Fact]
        public async Task TestForEachNumberExecutesDonePort()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("forEach", typeof(AdvancedIterationNodes), "ForEachNumber")
                .MapInput("collection", "[1,2,3,4,5]")
                .WithOutputPorts("item", "done")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("forEach");

            Assert.Equal(5, result.GetOutput<int>("forEach", "ItemCount"));
        }

        [Fact]
        public async Task TestForEachWithEmptyCollection()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("forEach", typeof(AdvancedIterationNodes), "ForEachString")
                .MapInput("collection", "[]")
                .WithOutputPorts("item", "done")
                .AutoMapOutputs();

            graph.AddNode("afterLoop", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "42")
                .MapInput("input2", "0")
                .AutoMapOutputs();

            graph.Connect("forEach", "afterLoop", "done");

            var result = await graph.ExecuteAsync("forEach");

            // Done port should still execute even with empty collection
            Assert.Equal(0, result.GetOutput<int>("forEach", "ItemCount"));
            Assert.Equal(42, result.GetOutput<int>("afterLoop", "result"));
        }

        #endregion
    }
}
