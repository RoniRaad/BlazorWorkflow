using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Testing;
using Xunit;

namespace TestRunner
{
    /// <summary>
    /// Integration tests for complex workflow scenarios that combine multiple features.
    /// These tests simulate real-world usage patterns and catch integration issues.
    /// </summary>
    public class WorkflowIntegrationTests
    {
        #region Deep Nesting and Long Chains

        [Fact]
        public async Task TestDeepChainedCalculations()
        {
            // Test a very long chain: ((((1 + 2) * 3) + 4) * 5) + 6 = 51
            var graph = new NodeGraphBuilder();

            graph.AddNode("add1", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "1")
                .MapInput("input2", "2")
                .AutoMapOutputs();

            graph.AddNode("mult1", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "3")
                .AutoMapOutputs();

            graph.AddNode("add2", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")
                .MapInput("input2", "4")
                .AutoMapOutputs();

            graph.AddNode("mult2", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.AddNode("add3", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")
                .MapInput("input2", "6")
                .AutoMapOutputs();

            graph.Connect("add1", "mult1");
            graph.Connect("mult1", "add2");
            graph.Connect("add2", "mult2");
            graph.Connect("mult2", "add3");

            var result = await graph.ExecuteAsync("add1");

            Assert.Equal(3, result.GetOutput<int>("add1", "result"));    // 1 + 2
            Assert.Equal(9, result.GetOutput<int>("mult1", "result"));   // 3 * 3
            Assert.Equal(13, result.GetOutput<int>("add2", "result"));   // 9 + 4
            Assert.Equal(65, result.GetOutput<int>("mult2", "result"));  // 13 * 5
            Assert.Equal(71, result.GetOutput<int>("add3", "result"));   // 65 + 6
        }

        [Fact]
        public async Task TestTenNodeChain()
        {
            // Test a 10-node chain to ensure deep chains work
            var graph = new NodeGraphBuilder();

            graph.AddNode("n1", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "1")
                .MapInput("input2", "1")
                .AutoMapOutputs();

            for (int i = 2; i <= 10; i++)
            {
                graph.AddNode($"n{i}", typeof(BaseNodeCollection), "Add")
                    .MapInput("input1", "input.result")
                    .MapInput("input2", "1")
                    .AutoMapOutputs();

                graph.Connect($"n{i - 1}", $"n{i}");
            }

            var result = await graph.ExecuteAsync("n1");

            // Each node adds 1, starting from 2 (1+1)
            Assert.Equal(11, result.GetOutput<int>("n10", "result"));
        }

        #endregion

        #region Multiple Branches and Merges

        [Fact]
        public async Task TestMultipleBranchesFromSingleSource()
        {
            // One source feeds multiple independent calculation branches
            //       source (10)
            //      /    |    \
            //     A     B     C
            //    *2    +5    -3
            //    20    15     7

            var graph = new NodeGraphBuilder();

            graph.AddNode("source", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "5")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.AddNode("branchA", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "2")
                .AutoMapOutputs();

            graph.AddNode("branchB", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.AddNode("branchC", typeof(BaseNodeCollection), "Subtract")
                .MapInput("input1", "input.result")
                .MapInput("input2", "3")
                .AutoMapOutputs();

            graph.Connect("source", "branchA");
            graph.Connect("source", "branchB");
            graph.Connect("source", "branchC");

            var result = await graph.ExecuteAsync("source");

            Assert.Equal(10, result.GetOutput<int>("source", "result"));
            Assert.Equal(20, result.GetOutput<int>("branchA", "result"));
            Assert.Equal(15, result.GetOutput<int>("branchB", "result"));
            Assert.Equal(7, result.GetOutput<int>("branchC", "result"));
        }

        [Fact]
        public async Task TestDeeperBranchingTree()
        {
            // Tree structure with second level branching
            //           root
            //          /    \
            //        L1      R1
            //       / \      / \
            //     L2  L3   R2  R3

            var graph = new NodeGraphBuilder();

            graph.AddNode("root", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "100")
                .MapInput("input2", "0")
                .AutoMapOutputs();

            // Left branch level 1
            graph.AddNode("L1", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")
                .MapInput("input2", "10")
                .AutoMapOutputs();

            // Right branch level 1
            graph.AddNode("R1", typeof(BaseNodeCollection), "Subtract")
                .MapInput("input1", "input.result")
                .MapInput("input2", "10")
                .AutoMapOutputs();

            // Left branch level 2
            graph.AddNode("L2", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "2")
                .AutoMapOutputs();

            graph.AddNode("L3", typeof(BaseNodeCollection), "Divide")
                .MapInput("input1", "input.result")
                .MapInput("input2", "2")
                .AutoMapOutputs();

            // Right branch level 2
            graph.AddNode("R2", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "3")
                .AutoMapOutputs();

            graph.AddNode("R3", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")
                .MapInput("input2", "20")
                .AutoMapOutputs();

            graph.Connect("root", "L1");
            graph.Connect("root", "R1");
            graph.Connect("L1", "L2");
            graph.Connect("L1", "L3");
            graph.Connect("R1", "R2");
            graph.Connect("R1", "R3");

            var result = await graph.ExecuteAsync("root");

            Assert.Equal(100, result.GetOutput<int>("root", "result"));
            Assert.Equal(110, result.GetOutput<int>("L1", "result"));   // 100 + 10
            Assert.Equal(90, result.GetOutput<int>("R1", "result"));    // 100 - 10
            Assert.Equal(220, result.GetOutput<int>("L2", "result"));   // 110 * 2
            Assert.Equal(55, result.GetOutput<int>("L3", "result"));    // 110 / 2
            Assert.Equal(270, result.GetOutput<int>("R2", "result"));   // 90 * 3
            Assert.Equal(110, result.GetOutput<int>("R3", "result"));   // 90 + 20
        }

        #endregion

        #region Mixed Type Operations

        [Fact]
        public async Task TestMixedIntAndDoubleOperations()
        {
            // Start with int, convert to double, back to int
            var graph = new NodeGraphBuilder();

            graph.AddNode("intAdd", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "6")
                .AutoMapOutputs();

            graph.AddNode("sqrt", typeof(BaseNodeCollection), "Sqrt")
                .MapInput("value", "input.result")
                .AutoMapOutputs();

            graph.AddNode("doubleMultiply", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "input.result")
                .MapInput("input2", "2.5")
                .AutoMapOutputs();

            graph.AddNode("comparison", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "input.result")
                .MapInput("b", "9")
                .AutoMapOutputs();

            graph.Connect("intAdd", "sqrt");
            graph.Connect("sqrt", "doubleMultiply");
            graph.Connect("doubleMultiply", "comparison");

            var result = await graph.ExecuteAsync("intAdd");

            Assert.Equal(16, result.GetOutput<int>("intAdd", "result"));
            Assert.Equal(4.0, result.GetOutput<double>("sqrt", "result"));
            Assert.Equal(10.0, result.GetOutput<double>("doubleMultiply", "result"));
            Assert.True(result.GetOutput<bool>("comparison", "result"));  // 10.0 > 9
        }

        [Fact]
        public async Task TestStringToNumberConversionChain()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("concat", typeof(BaseNodeCollection), "StringConcat")
                .MapInput("input1", "12")
                .MapInput("input2", "34")
                .AutoMapOutputs();

            graph.AddNode("parseInt", typeof(BaseNodeCollection), "ParseInt")
                .MapInput("input", "input.result")
                .AutoMapOutputs();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")
                .MapInput("input2", "100")
                .AutoMapOutputs();

            graph.Connect("concat", "parseInt");
            graph.Connect("parseInt", "add");

            var result = await graph.ExecuteAsync("concat");

            Assert.Equal("1234", result.GetOutput<string>("concat", "result"));
            Assert.Equal(1234, result.GetOutput<int>("parseInt", "result"));
            Assert.Equal(1334, result.GetOutput<int>("add", "result"));
        }

        #endregion

        #region Comparison Chains

        [Fact]
        public async Task TestChainedComparisons()
        {
            // Test: (10 > 5) AND (20 < 30)
            var graph = new NodeGraphBuilder();

            graph.AddNode("greater", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "10")
                .MapInput("b", "5")
                .AutoMapOutputs();

            graph.AddNode("less", typeof(CoreNodes), "LessThan")
                .MapInput("a", "20")
                .MapInput("b", "30")
                .AutoMapOutputs();

            graph.AddNode("and", typeof(CoreNodes), "And")
                .MapInput("a", "input.result")
                .MapInput("b", "true")
                .AutoMapOutputs();

            graph.Connect("greater", "and");

            var result = await graph.ExecuteAsync("greater");

            Assert.True(result.GetOutput<bool>("greater", "result"));
            Assert.True(result.GetOutput<bool>("less", "result"));
            Assert.True(result.GetOutput<bool>("and", "result"));
        }

        [Fact]
        public async Task TestComplexBooleanLogic()
        {
            // Test: (true AND false) OR (NOT false) = true
            var graph = new NodeGraphBuilder();

            graph.AddNode("and", typeof(CoreNodes), "And")
                .MapInput("a", "true")
                .MapInput("b", "false")
                .AutoMapOutputs();

            graph.AddNode("not", typeof(CoreNodes), "Not")
                .MapInput("value", "false")
                .AutoMapOutputs();

            graph.AddNode("or", typeof(CoreNodes), "Or")
                .MapInput("a", "input.result")
                .MapInput("b", "true")
                .AutoMapOutputs();

            graph.Connect("and", "or");

            var result = await graph.ExecuteAsync("and");

            Assert.False(result.GetOutput<bool>("and", "result"));
            Assert.True(result.GetOutput<bool>("not", "result"));
            Assert.True(result.GetOutput<bool>("or", "result"));
        }

        #endregion

        #region Real-World Calculation Scenarios

        [Fact]
        public async Task TestCompoundInterestCalculation()
        {
            // Calculate: P * (1 + r)^t
            // P = 1000, r = 0.05, t = 2
            // 1000 * (1.05)^2 = 1102.5
            var graph = new NodeGraphBuilder();

            // 1 + r = 1.05
            graph.AddNode("addRate", typeof(BaseNodeCollection), "AddD")
                .MapInput("input1", "1")
                .MapInput("input2", "0.05")
                .AutoMapOutputs();

            // (1 + r)^t = 1.05^2
            graph.AddNode("power", typeof(BaseNodeCollection), "Pow")
                .MapInput("base", "input.result")
                .MapInput("exponent", "2")
                .AutoMapOutputs();

            // P * result = 1000 * 1.1025
            graph.AddNode("multiply", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "1000")
                .MapInput("input2", "input.result")
                .AutoMapOutputs();

            graph.Connect("addRate", "power");
            graph.Connect("power", "multiply");

            var result = await graph.ExecuteAsync("addRate");

            Assert.Equal(1.05, result.GetOutput<double>("addRate", "result"));
            Assert.Equal(1.1025, result.GetOutput<double>("power", "result"), 4);
            Assert.Equal(1102.5, result.GetOutput<double>("multiply", "result"), 2);
        }

        [Fact]
        public async Task TestQuadraticFormula()
        {
            // Solve: x^2 - 5x + 6 = 0
            // Using: x = (-b + sqrt(b^2 - 4ac)) / 2a
            // a=1, b=-5, c=6
            // x = (5 + sqrt(25 - 24)) / 2 = (5 + 1) / 2 = 3
            var graph = new NodeGraphBuilder();

            // b^2 = 25
            graph.AddNode("bSquared", typeof(BaseNodeCollection), "Pow")
                .MapInput("base", "-5")
                .MapInput("exponent", "2")
                .AutoMapOutputs();

            // 4ac = 24
            graph.AddNode("fourAC", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "4")
                .MapInput("input2", "6")
                .AutoMapOutputs();

            // b^2 - 4ac = 1
            graph.AddNode("discriminant", typeof(BaseNodeCollection), "SubtractD")
                .MapInput("input1", "input.result")
                .MapInput("input2", "24")
                .AutoMapOutputs();

            graph.Connect("bSquared", "discriminant");

            // sqrt(discriminant)
            graph.AddNode("sqrtDisc", typeof(BaseNodeCollection), "Sqrt")
                .MapInput("value", "input.result")
                .AutoMapOutputs();

            graph.Connect("discriminant", "sqrtDisc");

            // -b + sqrt = 5 + 1 = 6
            graph.AddNode("numerator", typeof(BaseNodeCollection), "AddD")
                .MapInput("input1", "5")
                .MapInput("input2", "input.result")
                .AutoMapOutputs();

            graph.Connect("sqrtDisc", "numerator");

            // / 2a = 6 / 2 = 3
            graph.AddNode("solution", typeof(BaseNodeCollection), "DivideD")
                .MapInput("numerator", "input.result")
                .MapInput("denominator", "2")
                .AutoMapOutputs();

            graph.Connect("numerator", "solution");

            var result = await graph.ExecuteAsync("bSquared");

            Assert.Equal(25.0, result.GetOutput<double>("bSquared", "result"));
            Assert.Equal(1.0, result.GetOutput<double>("discriminant", "result"));
            Assert.Equal(1.0, result.GetOutput<double>("sqrtDisc", "result"));
            Assert.Equal(6.0, result.GetOutput<double>("numerator", "result"));
            Assert.Equal(3.0, result.GetOutput<double>("solution", "result"));
        }

        [Fact]
        public async Task TestBMICalculation()
        {
            // BMI = weight(kg) / height(m)^2
            // weight = 70kg, height = 1.75m
            // BMI = 70 / 3.0625 = 22.86
            var graph = new NodeGraphBuilder();

            // height^2
            graph.AddNode("heightSquared", typeof(BaseNodeCollection), "Pow")
                .MapInput("base", "1.75")
                .MapInput("exponent", "2")
                .AutoMapOutputs();

            // BMI
            graph.AddNode("bmi", typeof(BaseNodeCollection), "DivideD")
                .MapInput("numerator", "70")
                .MapInput("denominator", "input.result")
                .AutoMapOutputs();

            graph.Connect("heightSquared", "bmi");

            var result = await graph.ExecuteAsync("heightSquared");

            Assert.Equal(3.0625, result.GetOutput<double>("heightSquared", "result"));
            Assert.InRange(result.GetOutput<double>("bmi", "result"), 22.85, 22.87);
        }

        #endregion

        #region Conditional Logic Workflows

        [Fact]
        public async Task TestGradeCalculationWithComparison()
        {
            // Calculate percentage then check if passing
            // score: 85, max: 100, passing: 60
            var graph = new NodeGraphBuilder();

            // Calculate percentage: (85/100) * 100 = 85
            graph.AddNode("divide", typeof(BaseNodeCollection), "DivideD")
                .MapInput("numerator", "85")
                .MapInput("denominator", "100")
                .AutoMapOutputs();

            graph.AddNode("percentage", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "input.result")
                .MapInput("input2", "100")
                .AutoMapOutputs();

            graph.Connect("divide", "percentage");

            // Check if passing (>= 60)
            graph.AddNode("isPassing", typeof(CoreNodes), "GreaterOrEqual")
                .MapInput("a", "input.result")
                .MapInput("b", "60")
                .AutoMapOutputs();

            graph.Connect("percentage", "isPassing");

            var result = await graph.ExecuteAsync("divide");

            Assert.Equal(85.0, result.GetOutput<double>("percentage", "result"));
            Assert.True(result.GetOutput<bool>("isPassing", "result"));
        }

        [Fact]
        public async Task TestAgeVerificationWorkflow()
        {
            // Check: age >= 18 AND age < 65
            var graph = new NodeGraphBuilder();

            graph.AddNode("ageAtLeast18", typeof(CoreNodes), "GreaterOrEqual")
                .MapInput("a", "25")
                .MapInput("b", "18")
                .AutoMapOutputs();

            graph.AddNode("ageLess65", typeof(CoreNodes), "LessThan")
                .MapInput("a", "25")
                .MapInput("b", "65")
                .AutoMapOutputs();

            graph.AddNode("isWorkingAge", typeof(CoreNodes), "And")
                .MapInput("a", "input.result")
                .MapInput("b", "true")
                .AutoMapOutputs();

            graph.Connect("ageAtLeast18", "isWorkingAge");

            var result = await graph.ExecuteAsync("ageAtLeast18");

            Assert.True(result.GetOutput<bool>("ageAtLeast18", "result"));
            Assert.True(result.GetOutput<bool>("ageLess65", "result"));
            Assert.True(result.GetOutput<bool>("isWorkingAge", "result"));
        }

        #endregion

        #region String Processing Workflows

        [Fact]
        public async Task TestFullNameFormattingWorkflow()
        {
            // Build full name: firstName + " " + lastName
            var graph = new NodeGraphBuilder();

            graph.AddNode("addSpace", typeof(BaseNodeCollection), "StringConcat")
                .MapInput("input1", "John")
                .MapInput("input2", " ")
                .AutoMapOutputs();

            graph.AddNode("addLastName", typeof(BaseNodeCollection), "StringConcat")
                .MapInput("input1", "input.result")
                .MapInput("input2", "Doe")
                .AutoMapOutputs();

            graph.AddNode("toUpper", typeof(BaseNodeCollection), "ToUpper")
                .MapInput("input", "input.result")
                .AutoMapOutputs();

            graph.Connect("addSpace", "addLastName");
            graph.Connect("addLastName", "toUpper");

            var result = await graph.ExecuteAsync("addSpace");

            Assert.Equal("John ", result.GetOutput<string>("addSpace", "result"));
            Assert.Equal("John Doe", result.GetOutput<string>("addLastName", "result"));
            Assert.Equal("JOHN DOE", result.GetOutput<string>("toUpper", "result"));
        }

        [Fact]
        public async Task TestStringLengthValidation()
        {
            // Check if string length is within range (5-20 chars)
            var graph = new NodeGraphBuilder();

            graph.AddNode("length", typeof(BaseNodeCollection), "Length")
                .MapInput("input", "HelloWorld")
                .AutoMapOutputs();

            graph.AddNode("atLeast5", typeof(CoreNodes), "GreaterOrEqual")
                .MapInput("a", "input.result")
                .MapInput("b", "5")
                .AutoMapOutputs();

            graph.AddNode("atMost20", typeof(CoreNodes), "LessOrEqual")
                .MapInput("a", "input.result")
                .MapInput("b", "20")
                .AutoMapOutputs();

            graph.Connect("length", "atLeast5");
            graph.Connect("length", "atMost20");

            var result = await graph.ExecuteAsync("length");

            Assert.Equal(10, result.GetOutput<int>("length", "result"));
            Assert.True(result.GetOutput<bool>("atLeast5", "result"));
            Assert.True(result.GetOutput<bool>("atMost20", "result"));
        }

        #endregion

        #region Performance and Stress Tests

        [Fact]
        public async Task TestWideFanout()
        {
            // One source feeding 20 consumers
            var graph = new NodeGraphBuilder();

            graph.AddNode("source", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "50")
                .MapInput("input2", "50")
                .AutoMapOutputs();

            for (int i = 1; i <= 20; i++)
            {
                graph.AddNode($"consumer{i}", typeof(BaseNodeCollection), "Add")
                    .MapInput("input1", "input.result")
                    .MapInput("input2", $"{i}")
                    .AutoMapOutputs();

                graph.Connect("source", $"consumer{i}");
            }

            var result = await graph.ExecuteAsync("source");

            Assert.Equal(100, result.GetOutput<int>("source", "result"));
            for (int i = 1; i <= 20; i++)
            {
                Assert.Equal(100 + i, result.GetOutput<int>($"consumer{i}", "result"));
            }
        }

        #endregion

        #region Error Recovery Scenarios

        [Fact]
        public async Task TestDivisionResultUsedInComparison()
        {
            // Ensure division result properly converts for comparison
            var graph = new NodeGraphBuilder();

            graph.AddNode("divide", typeof(BaseNodeCollection), "DivideD")
                .MapInput("numerator", "100")
                .MapInput("denominator", "4")
                .AutoMapOutputs();

            graph.AddNode("equal", typeof(CoreNodes), "Equal")
                .MapInput("a", "input.result")
                .MapInput("b", "25")
                .AutoMapOutputs();

            graph.Connect("divide", "equal");

            var result = await graph.ExecuteAsync("divide");

            Assert.Equal(25.0, result.GetOutput<double>("divide", "result"));
            Assert.True(result.GetOutput<bool>("equal", "result"));
        }

        #endregion
    }
}
