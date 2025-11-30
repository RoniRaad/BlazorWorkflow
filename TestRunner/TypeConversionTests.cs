using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Testing;
using Xunit;

namespace TestRunner
{
    /// <summary>
    /// Tests to verify type conversion works correctly when passing data between nodes.
    /// This is critical because nodes receive inputs as JsonElement and must convert them properly.
    /// </summary>
    public class TypeConversionTests
    {
        #region String to Number Conversions

        [Fact]
        public async Task TestStringToIntConversion()
        {
            // Test that string "42" gets converted to int properly
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "42")      // String literal
                .MapInput("input2", "10")      // String literal
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("add");
            var sum = result.GetOutput<int>("add", "result");

            Assert.Equal(52, sum);
        }

        [Fact]
        public async Task TestStringToDoubleConversion()
        {
            // Test that string "3.14" gets converted to double properly
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "AddD")
                .MapInput("input1", "3.14")
                .MapInput("input2", "2.86")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("add");
            var sum = result.GetOutput<double>("add", "result");

            Assert.Equal(6.0, sum, 2);
        }

        [Fact]
        public async Task TestMixedNumericTypes()
        {
            // Verify int can work with operations expecting numbers
            var graph = new NodeGraphBuilder();

            // Add two ints
            graph.AddNode("addInts", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "100")
                .MapInput("input2", "50")
                .AutoMapOutputs();

            // Convert int result to double and do double math
            graph.AddNode("multiplyDouble", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "input.result")  // int from previous node
                .MapInput("input2", "1.5")           // double
                .AutoMapOutputs();

            graph.Connect("addInts", "multiplyDouble");

            var result = await graph.ExecuteAsync("addInts");

            Assert.Equal(150, result.GetOutput<int>("addInts", "result"));
            Assert.Equal(225.0, result.GetOutput<double>("multiplyDouble", "result"), 2);
        }

        #endregion

        #region Boolean Conversions

        [Fact]
        public async Task TestStringToBooleanComparison()
        {
            // Test that comparison nodes properly convert string numbers to numeric values
            var graph = new NodeGraphBuilder();

            graph.AddNode("compare", typeof(CoreNodes), "Equal")
                .MapInput("a", "42")
                .MapInput("b", "42")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("compare");
            Assert.True(result.GetOutput<bool>("compare", "result"));
        }

        [Fact]
        public async Task TestBooleanLogicChain()
        {
            // Test boolean outputs flow correctly through logic nodes
            var graph = new NodeGraphBuilder();

            graph.AddNode("compare1", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "100")
                .MapInput("b", "50")
                .AutoMapOutputs();

            graph.AddNode("compare2", typeof(CoreNodes), "LessThan")
                .MapInput("a", "10")
                .MapInput("b", "20")
                .AutoMapOutputs();

            graph.AddNode("andNode", typeof(CoreNodes), "And")
                .MapInput("a", "input.result")  // From compare1
                .MapInput("b", "input.result")  // From compare2
                .AutoMapOutputs();

            graph.Connect("compare1", "andNode");
            graph.Connect("compare2", "andNode");

            var result = await graph.ExecuteAsync("compare1");

            Assert.True(result.GetOutput<bool>("compare1", "result"));
            Assert.True(result.GetOutput<bool>("compare2", "result"));
            Assert.True(result.GetOutput<bool>("andNode", "result"));
        }

        #endregion

        #region String Conversions

        [Fact]
        public async Task TestNumericToStringConversion()
        {
            // Convert int to string
            var graph = new NodeGraphBuilder();

            graph.AddNode("intToString", typeof(BaseNodeCollection), "IntToString")
                .MapInput("value", "42")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("intToString");
            Assert.Equal("42", result.GetOutput<string>("intToString", "result"));
        }

        [Fact]
        public async Task TestStringConcatenationWithNumbers()
        {
            // Test that string concat handles various input types
            var graph = new NodeGraphBuilder();

            graph.AddNode("concat", typeof(BaseNodeCollection), "StringConcat")
                .MapInput("input1", "Value: ")
                .MapInput("input2", "42")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("concat");
            Assert.Equal("Value: 42", result.GetOutput<string>("concat", "result"));
        }

        [Fact]
        public async Task TestStringLengthAfterTransformation()
        {
            // Chain: ToUpper -> Length
            var graph = new NodeGraphBuilder();

            graph.AddNode("upper", typeof(BaseNodeCollection), "ToUpper")
                .MapInput("input", "hello world")
                .AutoMapOutputs();

            graph.AddNode("length", typeof(BaseNodeCollection), "Length")
                .MapInput("input", "input.result")
                .AutoMapOutputs();

            graph.Connect("upper", "length");

            var result = await graph.ExecuteAsync("upper");

            Assert.Equal("HELLO WORLD", result.GetOutput<string>("upper", "result"));
            Assert.Equal(11, result.GetOutput<int>("length", "result"));
        }

        #endregion

        #region Complex Type Conversions

        [Fact]
        public async Task TestIntToDoubleAndBack()
        {
            // Int -> Double -> Int round trip
            var graph = new NodeGraphBuilder();

            graph.AddNode("intToDouble", typeof(BaseNodeCollection), "IntToDouble")
                .MapInput("value", "42")
                .AutoMapOutputs();

            graph.AddNode("doubleToInt", typeof(BaseNodeCollection), "DoubleToInt")
                .MapInput("value", "input.result")
                .AutoMapOutputs();

            graph.Connect("intToDouble", "doubleToInt");

            var result = await graph.ExecuteAsync("intToDouble");

            Assert.Equal(42.0, result.GetOutput<double>("intToDouble", "result"));
            Assert.Equal(42, result.GetOutput<int>("doubleToInt", "result"));
        }

        [Fact]
        public async Task TestBoolToIntConversion()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("compare", typeof(CoreNodes), "Equal")
                .MapInput("a", "5")
                .MapInput("b", "5")
                .AutoMapOutputs();

            graph.AddNode("boolToInt", typeof(BaseNodeCollection), "BoolToInt")
                .MapInput("value", "input.result")
                .AutoMapOutputs();

            graph.Connect("compare", "boolToInt");

            var result = await graph.ExecuteAsync("compare");

            Assert.True(result.GetOutput<bool>("compare", "result"));
            Assert.Equal(1, result.GetOutput<int>("boolToInt", "result"));
        }

        [Fact]
        public async Task TestIntToBoolConversion()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "1")
                .MapInput("input2", "1")
                .AutoMapOutputs();

            graph.AddNode("intToBool", typeof(BaseNodeCollection), "IntToBool")
                .MapInput("value", "input.result")
                .AutoMapOutputs();

            graph.Connect("add", "intToBool");

            var result = await graph.ExecuteAsync("add");

            Assert.Equal(2, result.GetOutput<int>("add", "result"));
            Assert.True(result.GetOutput<bool>("intToBool", "result")); // 2 != 0, so true
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task TestZeroConversions()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("intToBool", typeof(BaseNodeCollection), "IntToBool")
                .MapInput("value", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("intToBool");
            Assert.False(result.GetOutput<bool>("intToBool", "result")); // 0 == false
        }

        [Fact]
        public async Task TestNegativeNumbers()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("abs", typeof(BaseNodeCollection), "Abs")
                .MapInput("value", "-42")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("abs");
            Assert.Equal(42, result.GetOutput<int>("abs", "result"));
        }

        [Fact]
        public async Task TestLargeNumbers()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("multiply", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "1000000")
                .MapInput("input2", "1000")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("multiply");
            Assert.Equal(1000000000, result.GetOutput<int>("multiply", "result"));
        }

        [Fact]
        public async Task TestDecimalPrecision()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("divide", typeof(BaseNodeCollection), "DivideD")
                .MapInput("numerator", "1")
                .MapInput("denominator", "3")
                .AutoMapOutputs();

            graph.AddNode("multiply", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "input.result")
                .MapInput("input2", "3")
                .AutoMapOutputs();

            graph.Connect("divide", "multiply");

            var result = await graph.ExecuteAsync("divide");

            var divResult = result.GetOutput<double>("divide", "result");
            var mulResult = result.GetOutput<double>("multiply", "result");

            Assert.Equal(0.333, divResult, 3);
            Assert.Equal(1.0, mulResult, 10); // Should be very close to 1.0
        }

        #endregion

        #region Comparison Type Handling

        [Fact]
        public async Task TestComparisonWithDifferentStringNumbers()
        {
            // "100" vs "50" as strings should compare as numbers, not alphabetically
            var graph = new NodeGraphBuilder();

            graph.AddNode("gt", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "100")
                .MapInput("b", "50")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("gt");
            Assert.True(result.GetOutput<bool>("gt", "result"));
        }

        [Fact]
        public async Task TestComparisonWithLeadingZeros()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("equal", typeof(CoreNodes), "Equal")
                .MapInput("a", "042")
                .MapInput("b", "42")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equal");
            Assert.True(result.GetOutput<bool>("equal", "result"));
        }

        [Fact]
        public async Task TestComparisonChain()
        {
            // Test: (a > b) AND (c < d) AND (e == f)
            var graph = new NodeGraphBuilder();

            graph.AddNode("gt", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "100")
                .MapInput("b", "50")
                .AutoMapOutputs();

            graph.AddNode("lt", typeof(CoreNodes), "LessThan")
                .MapInput("a", "25")
                .MapInput("b", "75")
                .AutoMapOutputs();

            graph.AddNode("eq", typeof(CoreNodes), "Equal")
                .MapInput("a", "42")
                .MapInput("b", "42")
                .AutoMapOutputs();

            graph.AddNode("and1", typeof(CoreNodes), "And")
                .MapInput("a", "input.result")
                .MapInput("b", "input.result")
                .AutoMapOutputs();

            graph.AddNode("and2", typeof(CoreNodes), "And")
                .MapInput("a", "input.result")
                .MapInput("b", "input.result")
                .AutoMapOutputs();

            graph.Connect("gt", "and1");
            graph.Connect("lt", "and1");
            graph.Connect("and1", "and2");
            graph.Connect("eq", "and2");

            var result = await graph.ExecuteAsync("gt");

            Assert.True(result.GetOutput<bool>("gt", "result"));
            Assert.True(result.GetOutput<bool>("lt", "result"));
            Assert.True(result.GetOutput<bool>("eq", "result"));
            Assert.True(result.GetOutput<bool>("and1", "result"));
            Assert.True(result.GetOutput<bool>("and2", "result"));
        }

        #endregion

        #region String Parsing Edge Cases

        [Fact]
        public async Task TestParseIntWithValidString()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("parse", typeof(BaseNodeCollection), "ParseInt")
                .MapInput("text", "12345")
                .MapInput("default", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("parse");
            Assert.Equal(12345, result.GetOutput<int>("parse", "result"));
        }

        [Fact]
        public async Task TestParseIntWithInvalidString()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("parse", typeof(BaseNodeCollection), "ParseInt")
                .MapInput("text", "not a number")
                .MapInput("default", "999")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("parse");
            Assert.Equal(999, result.GetOutput<int>("parse", "result")); // Should return default
        }

        [Fact]
        public async Task TestParseDoubleWithValidString()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("parse", typeof(BaseNodeCollection), "ParseDouble")
                .MapInput("text", "123.456")
                .MapInput("default", "0.0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("parse");
            Assert.Equal(123.456, result.GetOutput<double>("parse", "result"), 3);
        }

        [Fact]
        public async Task TestParseBoolWithValidString()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("parseTrue", typeof(BaseNodeCollection), "ParseBool")
                .MapInput("text", "true")
                .MapInput("default", "false")
                .AutoMapOutputs();

            graph.AddNode("parseFalse", typeof(BaseNodeCollection), "ParseBool")
                .MapInput("text", "false")
                .MapInput("default", "true")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("parseTrue");

            Assert.True(result.GetOutput<bool>("parseTrue", "result"));

            result = await graph.ExecuteAsync("parseFalse");
            Assert.False(result.GetOutput<bool>("parseFalse", "result"));
        }

        #endregion

        #region Math Operation Type Consistency

        [Fact]
        public async Task TestMathOperationChainTypeSafety()
        {
            // Ensure math operations maintain proper types through chain
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "20")
                .AutoMapOutputs();

            graph.AddNode("multiply", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "2")
                .AutoMapOutputs();

            graph.AddNode("subtract", typeof(BaseNodeCollection), "Subtract")
                .MapInput("input1", "input.result")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.AddNode("divide", typeof(BaseNodeCollection), "Divide")
                .MapInput("numerator", "input.result")
                .MapInput("denominator", "5")
                .AutoMapOutputs();

            graph.Connect("add", "multiply");
            graph.Connect("multiply", "subtract");
            graph.Connect("subtract", "divide");

            var result = await graph.ExecuteAsync("add");

            // 10 + 20 = 30
            Assert.Equal(30, result.GetOutput<int>("add", "result"));
            // 30 * 2 = 60
            Assert.Equal(60, result.GetOutput<int>("multiply", "result"));
            // 60 - 5 = 55
            Assert.Equal(55, result.GetOutput<int>("subtract", "result"));
            // 55 / 5 = 11
            Assert.Equal(11, result.GetOutput<int>("divide", "result"));
        }

        [Fact]
        public async Task TestFloatingPointChainTypeSafety()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "AddD")
                .MapInput("input1", "10.5")
                .MapInput("input2", "20.3")
                .AutoMapOutputs();

            graph.AddNode("multiply", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "input.result")
                .MapInput("input2", "2.0")
                .AutoMapOutputs();

            graph.AddNode("sqrt", typeof(BaseNodeCollection), "Sqrt")
                .MapInput("value", "input.result")
                .AutoMapOutputs();

            graph.Connect("add", "multiply");
            graph.Connect("multiply", "sqrt");

            var result = await graph.ExecuteAsync("add");

            var addResult = result.GetOutput<double>("add", "result");
            var multiplyResult = result.GetOutput<double>("multiply", "result");
            var sqrtResult = result.GetOutput<double>("sqrt", "result");

            Assert.Equal(30.8, addResult, 2);
            Assert.Equal(61.6, multiplyResult, 2);
            Assert.Equal(7.849, sqrtResult, 3);
        }

        #endregion
    }
}
