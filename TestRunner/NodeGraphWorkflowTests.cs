using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Testing;
using Xunit;

namespace TestRunner
{
    /// <summary>
    /// Comprehensive tests using NodeGraphBuilder to test actual node execution.
    /// These tests verify that the workflow engine correctly executes node graphs.
    /// </summary>
    public class NodeGraphWorkflowTests
    {
        #region Basic Arithmetic Tests

        [Fact]
        public async Task TestSimpleAddition()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "20")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("add");
            var sum = result.GetOutput<int>("add", "result");

            Assert.Equal(30, sum);
        }

        [Fact]
        public async Task TestMultiplication()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("multiply", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "7")
                .MapInput("input2", "6")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("multiply");
            var product = result.GetOutput<int>("multiply", "result");

            Assert.Equal(42, product);
        }

        [Fact]
        public async Task TestSubtraction()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("subtract", typeof(BaseNodeCollection), "Subtract")
                .MapInput("input1", "100")
                .MapInput("input2", "42")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("subtract");
            var difference = result.GetOutput<int>("subtract", "result");

            Assert.Equal(58, difference);
        }

        [Fact]
        public async Task TestDivision()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("divide", typeof(BaseNodeCollection), "Divide")
                .MapInput("numerator", "84")
                .MapInput("denominator", "2")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("divide");
            var quotient = result.GetOutput<int>("divide", "result");

            Assert.Equal(42, quotient);
        }

        #endregion

        #region Chained Node Execution

        [Fact]
        public async Task TestTwoNodesChained()
        {
            // Test: (5 + 10) * 3 = 45
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "5")
                .MapInput("input2", "10")
                .AutoMapOutputs();

            graph.AddNode("multiply", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")  // Get result from previous node
                .MapInput("input2", "3")
                .AutoMapOutputs();

            graph.Connect("add", "multiply");

            var result = await graph.ExecuteAsync("add");

            var addResult = result.GetOutput<int>("add", "result");
            var multiplyResult = result.GetOutput<int>("multiply", "result");

            Assert.Equal(15, addResult);
            Assert.Equal(45, multiplyResult);
        }

        [Fact]
        public async Task TestThreeNodesChained()
        {
            // Test: ((10 + 5) * 2) - 10 = 20
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.AddNode("multiply", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "2")
                .AutoMapOutputs();

            graph.AddNode("subtract", typeof(BaseNodeCollection), "Subtract")
                .MapInput("input1", "input.result")
                .MapInput("input2", "10")
                .AutoMapOutputs();

            graph.Connect("add", "multiply");
            graph.Connect("multiply", "subtract");

            var result = await graph.ExecuteAsync("add");

            Assert.Equal(15, result.GetOutput<int>("add", "result"));
            Assert.Equal(30, result.GetOutput<int>("multiply", "result"));
            Assert.Equal(20, result.GetOutput<int>("subtract", "result"));
        }

        #endregion

        #region Boolean Logic Tests

        [Fact]
        public async Task TestAndLogic()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("and", typeof(CoreNodes), "And")
                .MapInput("a", "true")
                .MapInput("b", "true")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("and");
            Assert.True(result.GetOutput<bool>("and", "result"));
        }

        [Fact]
        public async Task TestOrLogic()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("or", typeof(CoreNodes), "Or")
                .MapInput("a", "false")
                .MapInput("b", "true")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("or");
            Assert.True(result.GetOutput<bool>("or", "result"));
        }

        [Fact]
        public async Task TestNotLogic()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("not", typeof(CoreNodes), "Not")
                .MapInput("value", "false")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("not");
            Assert.True(result.GetOutput<bool>("not", "result"));
        }

        [Fact]
        public async Task TestXorLogic()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("xor", typeof(CoreNodes), "Xor")
                .MapInput("a", "true")
                .MapInput("b", "false")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("xor");
            Assert.True(result.GetOutput<bool>("xor", "result"));
        }

        #endregion

        #region Comparison Tests

        [Fact]
        public async Task TestEqualComparison()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("equal", typeof(CoreNodes), "Equal")
                .MapInput("a", "42")
                .MapInput("b", "42")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equal");
            Assert.True(result.GetOutput<bool>("equal", "result"));
        }

        [Fact]
        public async Task TestNotEqualComparison()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("notEqual", typeof(CoreNodes), "NotEqual")
                .MapInput("a", "42")
                .MapInput("b", "24")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("notEqual");
            Assert.True(result.GetOutput<bool>("notEqual", "result"));
        }

        [Fact]
        public async Task TestGreaterThanComparison()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("greaterThan", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "100")
                .MapInput("b", "50")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("greaterThan");
            Assert.True(result.GetOutput<bool>("greaterThan", "result"));
        }

        [Fact]
        public async Task TestLessThanComparison()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("lessThan", typeof(CoreNodes), "LessThan")
                .MapInput("a", "25")
                .MapInput("b", "50")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("lessThan");
            Assert.True(result.GetOutput<bool>("lessThan", "result"));
        }

        #endregion

        #region String Operations

        [Fact]
        public async Task TestStringConcat()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("concat", typeof(BaseNodeCollection), "StringConcat")
                .MapInput("input1", "Hello")
                .MapInput("input2", " World")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("concat");
            Assert.Equal("Hello World", result.GetOutput<string>("concat", "result"));
        }

        [Fact]
        public async Task TestStringLength()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("length", typeof(BaseNodeCollection), "Length")
                .MapInput("input", "Hello")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("length");
            Assert.Equal(5, result.GetOutput<int>("length", "result"));
        }

        [Fact]
        public async Task TestToUpper()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("toUpper", typeof(BaseNodeCollection), "ToUpper")
                .MapInput("input", "hello")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("toUpper");
            Assert.Equal("HELLO", result.GetOutput<string>("toUpper", "result"));
        }

        [Fact]
        public async Task TestToLower()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("toLower", typeof(BaseNodeCollection), "ToLower")
                .MapInput("input", "HELLO")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("toLower");
            Assert.Equal("hello", result.GetOutput<string>("toLower", "result"));
        }

        #endregion

        #region Floating Point Math

        [Fact]
        public async Task TestDoubleAddition()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "AddD")
                .MapInput("input1", "10.5")
                .MapInput("input2", "20.3")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("add");
            Assert.Equal(30.8, result.GetOutput<double>("add", "result"), 2);
        }

        [Fact]
        public async Task TestSqrt()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("sqrt", typeof(BaseNodeCollection), "Sqrt")
                .MapInput("value", "16")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("sqrt");
            Assert.Equal(4.0, result.GetOutput<double>("sqrt", "result"));
        }

        [Fact]
        public async Task TestPower()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("pow", typeof(BaseNodeCollection), "Pow")
                .MapInput("base", "2")
                .MapInput("exponent", "10")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("pow");
            Assert.Equal(1024.0, result.GetOutput<double>("pow", "result"));
        }

        #endregion

        #region Real-World Workflow Tests

        [Fact]
        public async Task TestTemperatureConversion()
        {
            // Convert Celsius to Fahrenheit: F = (C * 9/5) + 32
            // Example: 100°C = 212°F
            var graph = new NodeGraphBuilder();

            // Step 1: Multiply by 9
            graph.AddNode("multiplyBy9", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "100")
                .MapInput("input2", "9")
                .AutoMapOutputs();

            // Step 2: Divide by 5
            graph.AddNode("divideBy5", typeof(BaseNodeCollection), "DivideD")
                .MapInput("numerator", "input.result")
                .MapInput("denominator", "5")
                .AutoMapOutputs();

            // Step 3: Add 32
            graph.AddNode("add32", typeof(BaseNodeCollection), "AddD")
                .MapInput("input1", "input.result")
                .MapInput("input2", "32")
                .AutoMapOutputs();

            graph.Connect("multiplyBy9", "divideBy5");
            graph.Connect("divideBy5", "add32");

            var result = await graph.ExecuteAsync("multiplyBy9");

            Assert.Equal(900.0, result.GetOutput<double>("multiplyBy9", "result"));
            Assert.Equal(180.0, result.GetOutput<double>("divideBy5", "result"));
            Assert.Equal(212.0, result.GetOutput<double>("add32", "result"));
        }

        [Fact]
        public async Task TestDiscountCalculation()
        {
            // Calculate 20% discount on $100
            // Result: $100 * 0.8 = $80
            var graph = new NodeGraphBuilder();

            graph.AddNode("applyDiscount", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "100")
                .MapInput("input2", "0.8")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("applyDiscount");

            Assert.Equal(80.0, result.GetOutput<double>("applyDiscount", "result"));
        }

        [Fact]
        public async Task TestAreaOfCircle()
        {
            // Calculate area of circle with radius 10
            // Area = π * r² = 3.14159 * 100 = 314.159
            var graph = new NodeGraphBuilder();

            // Step 1: Square the radius (10^2 = 100)
            graph.AddNode("square", typeof(BaseNodeCollection), "Pow")
                .MapInput("base", "10")
                .MapInput("exponent", "2")
                .AutoMapOutputs();

            // Step 2: Multiply by π
            graph.AddNode("multiply", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "input.result")
                .MapInput("input2", "3.14159265359")
                .AutoMapOutputs();

            graph.Connect("square", "multiply");

            var result = await graph.ExecuteAsync("square");

            Assert.Equal(100.0, result.GetOutput<double>("square", "result"));
            Assert.Equal(314.159265359, result.GetOutput<double>("multiply", "result"), 5);
        }

        [Fact]
        public async Task TestComparisonWorkflow()
        {
            // Check if score >= 60 (passing grade)
            var graph = new NodeGraphBuilder();

            graph.AddNode("checkPass", typeof(CoreNodes), "GreaterOrEqual")
                .MapInput("a", "75")
                .MapInput("b", "60")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("checkPass");
            Assert.True(result.GetOutput<bool>("checkPass", "result"));
        }

        [Fact]
        public async Task TestStringProcessingWorkflow()
        {
            // Take a string, convert to upper, then concatenate with suffix
            var graph = new NodeGraphBuilder();

            graph.AddNode("toUpper", typeof(BaseNodeCollection), "ToUpper")
                .MapInput("input", "hello")
                .AutoMapOutputs();

            graph.AddNode("concat", typeof(BaseNodeCollection), "StringConcat")
                .MapInput("input1", "input.result")
                .MapInput("input2", " WORLD")
                .AutoMapOutputs();

            graph.Connect("toUpper", "concat");

            var result = await graph.ExecuteAsync("toUpper");

            Assert.Equal("HELLO", result.GetOutput<string>("toUpper", "result"));
            Assert.Equal("HELLO WORLD", result.GetOutput<string>("concat", "result"));
        }

        #endregion
    }
}
