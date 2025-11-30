using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Testing;
using Xunit;

namespace TestRunner
{
    /// <summary>
    /// Tests to verify JSON path input mapping works correctly.
    /// This is critical for data flow between nodes using "input.result" and nested paths.
    /// </summary>
    public class JsonPathInputTests
    {
        #region Basic Path Resolution

        [Fact]
        public async Task TestSimpleInputResultPath()
        {
            // Test that "input.result" correctly maps to previous node's output
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "20")
                .AutoMapOutputs();

            graph.AddNode("multiply", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")  // Path to previous node's result
                .MapInput("input2", "3")
                .AutoMapOutputs();

            graph.Connect("add", "multiply");

            var result = await graph.ExecuteAsync("add");

            Assert.Equal(30, result.GetOutput<int>("add", "result"));
            Assert.Equal(90, result.GetOutput<int>("multiply", "result"));
        }

        [Fact]
        public async Task TestMultiplePreviousNodeResults()
        {
            // Chain two operations: add then multiply
            var graph = new NodeGraphBuilder();

            graph.AddNode("add1", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.AddNode("multiply", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")  // From add1
                .MapInput("input2", "23")            // Literal
                .AutoMapOutputs();

            graph.Connect("add1", "multiply");

            var result = await graph.ExecuteAsync("add1");

            Assert.Equal(15, result.GetOutput<int>("add1", "result"));
            Assert.Equal(345, result.GetOutput<int>("multiply", "result")); // 15 * 23
        }

        #endregion

        #region Chained Path Resolution

        [Fact]
        public async Task TestThreeLevelChain()
        {
            // A -> B -> C, each using input.result
            var graph = new NodeGraphBuilder();

            graph.AddNode("nodeA", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "5")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.AddNode("nodeB", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "3")
                .AutoMapOutputs();

            graph.AddNode("nodeC", typeof(BaseNodeCollection), "Subtract")
                .MapInput("input1", "input.result")
                .MapInput("input2", "10")
                .AutoMapOutputs();

            graph.Connect("nodeA", "nodeB");
            graph.Connect("nodeB", "nodeC");

            var result = await graph.ExecuteAsync("nodeA");

            Assert.Equal(10, result.GetOutput<int>("nodeA", "result"));
            Assert.Equal(30, result.GetOutput<int>("nodeB", "result"));
            Assert.Equal(20, result.GetOutput<int>("nodeC", "result"));
        }

        [Fact]
        public async Task TestFiveLevelDeepChain()
        {
            // Test a deeper chain to ensure path resolution works at any depth
            var graph = new NodeGraphBuilder();

            graph.AddNode("n1", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "1")
                .MapInput("input2", "1")
                .AutoMapOutputs();

            graph.AddNode("n2", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")
                .MapInput("input2", "1")
                .AutoMapOutputs();

            graph.AddNode("n3", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")
                .MapInput("input2", "1")
                .AutoMapOutputs();

            graph.AddNode("n4", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")
                .MapInput("input2", "1")
                .AutoMapOutputs();

            graph.AddNode("n5", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")
                .MapInput("input2", "1")
                .AutoMapOutputs();

            graph.Connect("n1", "n2");
            graph.Connect("n2", "n3");
            graph.Connect("n3", "n4");
            graph.Connect("n4", "n5");

            var result = await graph.ExecuteAsync("n1");

            // Each node adds 1: 1+1=2, 2+1=3, 3+1=4, 4+1=5, 5+1=6
            Assert.Equal(2, result.GetOutput<int>("n1", "result"));
            Assert.Equal(3, result.GetOutput<int>("n2", "result"));
            Assert.Equal(4, result.GetOutput<int>("n3", "result"));
            Assert.Equal(5, result.GetOutput<int>("n4", "result"));
            Assert.Equal(6, result.GetOutput<int>("n5", "result"));
        }

        #endregion

        #region Mixed Path Types

        [Fact]
        public async Task TestMixedLiteralAndPathInputs()
        {
            // One input from literal, one from path
            var graph = new NodeGraphBuilder();

            graph.AddNode("source", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "7")
                .MapInput("input2", "8")
                .AutoMapOutputs();

            graph.AddNode("consumer", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")  // From source node
                .MapInput("input2", "100")           // Literal value
                .AutoMapOutputs();

            graph.Connect("source", "consumer");

            var result = await graph.ExecuteAsync("source");

            Assert.Equal(56, result.GetOutput<int>("source", "result"));
            Assert.Equal(156, result.GetOutput<int>("consumer", "result")); // 56 + 100
        }

        [Fact]
        public async Task TestBothInputsFromPaths()
        {
            // One input from path, one from literal
            var graph = new NodeGraphBuilder();

            graph.AddNode("source1", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.AddNode("consumer", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")  // From source1
                .MapInput("input2", "12")            // Literal
                .AutoMapOutputs();

            graph.Connect("source1", "consumer");

            var result = await graph.ExecuteAsync("source1");

            Assert.Equal(15, result.GetOutput<int>("source1", "result"));
            Assert.Equal(27, result.GetOutput<int>("consumer", "result")); // 15 + 12
        }

        #endregion

        #region Different Data Types Through Paths

        [Fact]
        public async Task TestStringThroughPath()
        {
            // Pass string through path
            var graph = new NodeGraphBuilder();

            graph.AddNode("upper", typeof(BaseNodeCollection), "ToUpper")
                .MapInput("input", "hello")
                .AutoMapOutputs();

            graph.AddNode("concat", typeof(BaseNodeCollection), "StringConcat")
                .MapInput("input1", "input.result")
                .MapInput("input2", " WORLD")
                .AutoMapOutputs();

            graph.Connect("upper", "concat");

            var result = await graph.ExecuteAsync("upper");

            Assert.Equal("HELLO", result.GetOutput<string>("upper", "result"));
            Assert.Equal("HELLO WORLD", result.GetOutput<string>("concat", "result"));
        }

        [Fact]
        public async Task TestBooleanThroughPath()
        {
            // Pass boolean through path
            var graph = new NodeGraphBuilder();

            graph.AddNode("compare", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "100")
                .MapInput("b", "50")
                .AutoMapOutputs();

            graph.AddNode("not", typeof(CoreNodes), "Not")
                .MapInput("value", "input.result")
                .AutoMapOutputs();

            graph.Connect("compare", "not");

            var result = await graph.ExecuteAsync("compare");

            Assert.True(result.GetOutput<bool>("compare", "result"));
            Assert.False(result.GetOutput<bool>("not", "result"));
        }

        [Fact]
        public async Task TestDoubleThroughPath()
        {
            // Pass double through path
            var graph = new NodeGraphBuilder();

            graph.AddNode("sqrt", typeof(BaseNodeCollection), "Sqrt")
                .MapInput("value", "16")
                .AutoMapOutputs();

            graph.AddNode("multiply", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "input.result")
                .MapInput("input2", "3.5")
                .AutoMapOutputs();

            graph.Connect("sqrt", "multiply");

            var result = await graph.ExecuteAsync("sqrt");

            Assert.Equal(4.0, result.GetOutput<double>("sqrt", "result"));
            Assert.Equal(14.0, result.GetOutput<double>("multiply", "result"));
        }

        #endregion

        #region Complex Data Flow Patterns

        [Fact]
        public async Task TestDiamondPattern()
        {
            // One source splits to two nodes, second uses result from first
            //     A
            //    / \
            //   B   C (uses B's result)

            var graph = new NodeGraphBuilder();

            graph.AddNode("A", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "20")
                .AutoMapOutputs();

            graph.AddNode("B", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "2")
                .AutoMapOutputs();

            graph.AddNode("C", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")  // From B
                .MapInput("input2", "30")            // Literal
                .AutoMapOutputs();

            graph.Connect("A", "B");
            graph.Connect("B", "C");

            var result = await graph.ExecuteAsync("A");

            Assert.Equal(30, result.GetOutput<int>("A", "result"));  // 10 + 20
            Assert.Equal(60, result.GetOutput<int>("B", "result"));  // 30 * 2
            Assert.Equal(90, result.GetOutput<int>("C", "result"));  // 60 + 30
        }

        [Fact]
        public async Task TestFanOutPattern()
        {
            // One source feeds three consumers
            var graph = new NodeGraphBuilder();

            graph.AddNode("source", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "5")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.AddNode("consumer1", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "2")
                .AutoMapOutputs();

            graph.AddNode("consumer2", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "3")
                .AutoMapOutputs();

            graph.AddNode("consumer3", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "4")
                .AutoMapOutputs();

            graph.Connect("source", "consumer1");
            graph.Connect("source", "consumer2");
            graph.Connect("source", "consumer3");

            var result = await graph.ExecuteAsync("source");

            Assert.Equal(10, result.GetOutput<int>("source", "result"));
            Assert.Equal(20, result.GetOutput<int>("consumer1", "result"));
            Assert.Equal(30, result.GetOutput<int>("consumer2", "result"));
            Assert.Equal(40, result.GetOutput<int>("consumer3", "result"));
        }

        [Fact]
        public async Task TestCascadingCalculation()
        {
            // Chain calculation: ((a + b) * c) - d
            var graph = new NodeGraphBuilder();

            // (a + b)
            graph.AddNode("addAB", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "20")
                .AutoMapOutputs();

            // * c
            graph.AddNode("multiplyC", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            // - d
            graph.AddNode("subtract", typeof(BaseNodeCollection), "Subtract")
                .MapInput("input1", "input.result")  // From multiplyC
                .MapInput("input2", "12")            // Literal
                .AutoMapOutputs();

            graph.Connect("addAB", "multiplyC");
            graph.Connect("multiplyC", "subtract");

            var result = await graph.ExecuteAsync("addAB");

            Assert.Equal(30, result.GetOutput<int>("addAB", "result"));      // 10 + 20
            Assert.Equal(150, result.GetOutput<int>("multiplyC", "result"));  // 30 * 5
            Assert.Equal(138, result.GetOutput<int>("subtract", "result"));   // 150 - 12
        }

        #endregion

        #region Type Conversion Through Paths

        [Fact]
        public async Task TestIntToDoubleConversionThroughPath()
        {
            // Int result feeds into double operation through path
            var graph = new NodeGraphBuilder();

            graph.AddNode("addInt", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "6")
                .AutoMapOutputs();

            graph.AddNode("sqrtDouble", typeof(BaseNodeCollection), "Sqrt")
                .MapInput("value", "input.result")  // Int converted to double
                .AutoMapOutputs();

            graph.Connect("addInt", "sqrtDouble");

            var result = await graph.ExecuteAsync("addInt");

            Assert.Equal(16, result.GetOutput<int>("addInt", "result"));
            Assert.Equal(4.0, result.GetOutput<double>("sqrtDouble", "result"));
        }

        [Fact]
        public async Task TestComparisonResultToLogicThroughPath()
        {
            // Comparison result (bool) feeds into logic operation
            var graph = new NodeGraphBuilder();

            graph.AddNode("compare1", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "100")
                .MapInput("b", "50")
                .AutoMapOutputs();

            graph.AddNode("compare2", typeof(CoreNodes), "Equal")
                .MapInput("a", "42")
                .MapInput("b", "42")
                .AutoMapOutputs();

            graph.AddNode("andLogic", typeof(CoreNodes), "And")
                .MapInput("a", "input.result")  // Bool from compare1
                .MapInput("b", "input.result")  // Bool from compare2
                .AutoMapOutputs();

            graph.Connect("compare1", "andLogic");
            graph.Connect("compare2", "andLogic");

            var result = await graph.ExecuteAsync("compare1");

            Assert.True(result.GetOutput<bool>("compare1", "result"));
            Assert.True(result.GetOutput<bool>("compare2", "result"));
            Assert.True(result.GetOutput<bool>("andLogic", "result"));
        }

        #endregion

        #region Real-World Scenarios

        [Fact]
        public async Task TestPayrollCalculationWithPaths()
        {
            // Calculate: (hourlyRate * hoursWorked) + overtime - deductions
            var graph = new NodeGraphBuilder();

            // Base pay: rate * hours
            graph.AddNode("basePay", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "25.50")  // Hourly rate
                .MapInput("input2", "40")     // Hours worked
                .AutoMapOutputs();

            // Add overtime
            graph.AddNode("withOvertime", typeof(BaseNodeCollection), "AddD")
                .MapInput("input1", "input.result")  // Base pay
                .MapInput("input2", "150.00")        // Overtime pay
                .AutoMapOutputs();

            // Subtract deductions
            graph.AddNode("netPay", typeof(BaseNodeCollection), "SubtractD")
                .MapInput("input1", "input.result")  // Gross pay
                .MapInput("input2", "250.00")        // Deductions
                .AutoMapOutputs();

            graph.Connect("basePay", "withOvertime");
            graph.Connect("withOvertime", "netPay");

            var result = await graph.ExecuteAsync("basePay");

            Assert.Equal(1020.0, result.GetOutput<double>("basePay", "result"), 2);        // 25.50 * 40
            Assert.Equal(1170.0, result.GetOutput<double>("withOvertime", "result"), 2);   // 1020 + 150
            Assert.Equal(920.0, result.GetOutput<double>("netPay", "result"), 2);          // 1170 - 250
        }

        [Fact]
        public async Task TestScoreNormalizationWithPaths()
        {
            // Normalize score: (score - min) / (max - min)
            var graph = new NodeGraphBuilder();

            // score - min = 75 - 50 = 25
            graph.AddNode("subtractMin", typeof(BaseNodeCollection), "SubtractD")
                .MapInput("input1", "75")   // Raw score
                .MapInput("input2", "50")   // Min score
                .AutoMapOutputs();

            // Divide by range: 25 / 50 = 0.5
            graph.AddNode("normalized", typeof(BaseNodeCollection), "DivideD")
                .MapInput("numerator", "input.result")    // From subtractMin (25)
                .MapInput("denominator", "50")            // Range (max - min)
                .AutoMapOutputs();

            graph.Connect("subtractMin", "normalized");

            var result = await graph.ExecuteAsync("subtractMin");

            Assert.Equal(25.0, result.GetOutput<double>("subtractMin", "result"));
            Assert.Equal(0.5, result.GetOutput<double>("normalized", "result"), 2);  // 25/50 = 0.5
        }

        [Fact]
        public async Task TestConditionalPricingWithPaths()
        {
            // Calculate price then apply discount
            var graph = new NodeGraphBuilder();

            // Base price: 19.99 * 12 = 239.88
            graph.AddNode("basePrice", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "19.99")  // Unit price
                .MapInput("input2", "12")     // Quantity
                .AutoMapOutputs();

            // Apply 10% discount: price * 0.9
            graph.AddNode("discountedPrice", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "input.result")  // Base price
                .MapInput("input2", "0.9")           // 90% (10% off)
                .AutoMapOutputs();

            graph.Connect("basePrice", "discountedPrice");

            var result = await graph.ExecuteAsync("basePrice");

            Assert.Equal(239.88, result.GetOutput<double>("basePrice", "result"), 2);
            Assert.Equal(215.89, result.GetOutput<double>("discountedPrice", "result"), 2);
        }

        #endregion
    }
}
