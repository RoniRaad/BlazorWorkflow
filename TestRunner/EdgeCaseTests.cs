using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Testing;
using Xunit;

namespace TestRunner
{
    /// <summary>
    /// Tests for edge cases, boundary conditions, and potential issues in type conversion,
    /// JSON handling, and node execution. These tests are designed to find bugs before users do.
    /// </summary>
    public class EdgeCaseTests
    {
        #region Null and Empty Value Handling

        [Fact]
        public async Task TestNullStringComparison()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("equal", typeof(CoreNodes), "Equal")
                .MapInput("a", "")
                .MapInput("b", "")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equal");
            Assert.True(result.GetOutput<bool>("equal", "result"));
        }

        [Fact]
        public async Task TestEmptyStringLength()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("length", typeof(BaseNodeCollection), "Length")
                .MapInput("input", "")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("length");
            Assert.Equal(0, result.GetOutput<int>("length", "result"));
        }

        [Fact]
        public async Task TestEmptyStringConcat()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("concat", typeof(BaseNodeCollection), "StringConcat")
                .MapInput("input1", "")
                .MapInput("input2", "test")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("concat");
            Assert.Equal("test", result.GetOutput<string>("concat", "result"));
        }

        #endregion

        #region Zero and Negative Number Handling

        [Fact]
        public async Task TestZeroComparison()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("equal", typeof(CoreNodes), "Equal")
                .MapInput("a", "0")
                .MapInput("b", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equal");
            Assert.True(result.GetOutput<bool>("equal", "result"));
        }

        [Fact]
        public async Task TestNegativeZeroEquality()
        {
            // -0.0 should equal 0.0
            var graph = new NodeGraphBuilder();

            graph.AddNode("equal", typeof(CoreNodes), "Equal")
                .MapInput("a", "-0.0")
                .MapInput("b", "0.0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equal");
            Assert.True(result.GetOutput<bool>("equal", "result"));
        }

        [Fact]
        public async Task TestNegativeNumberComparison()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("greater", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "-5")
                .MapInput("b", "-10")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("greater");
            Assert.True(result.GetOutput<bool>("greater", "result"));
        }

        [Fact]
        public async Task TestNegativeNumberAddition()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "-10")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("add");
            Assert.Equal(-5, result.GetOutput<int>("add", "result"));
        }

        [Fact]
        public async Task TestDivisionByOne()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("divide", typeof(BaseNodeCollection), "Divide")
                .MapInput("numerator", "42")
                .MapInput("denominator", "1")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("divide");
            Assert.Equal(42, result.GetOutput<int>("divide", "result"));
        }

        [Fact]
        public async Task TestMultiplicationByZero()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("multiply", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "999")
                .MapInput("input2", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("multiply");
            Assert.Equal(0, result.GetOutput<int>("multiply", "result"));
        }

        [Fact]
        public async Task TestAdditionWithZero()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "42")
                .MapInput("input2", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("add");
            Assert.Equal(42, result.GetOutput<int>("add", "result"));
        }

        #endregion

        #region Whitespace and Special Characters

        [Fact]
        public async Task TestStringWithWhitespace()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("concat", typeof(BaseNodeCollection), "StringConcat")
                .MapInput("input1", "  hello  ")
                .MapInput("input2", "  world  ")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("concat");
            Assert.Equal("  hello    world  ", result.GetOutput<string>("concat", "result"));
        }

        [Fact]
        public async Task TestNumberWithLeadingWhitespace()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "  10  ")
                .MapInput("input2", "  20  ")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("add");
            Assert.Equal(30, result.GetOutput<int>("add", "result"));
        }

        [Fact]
        public async Task TestStringEqualsWithWhitespace()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("equal", typeof(CoreNodes), "Equal")
                .MapInput("a", "hello ")
                .MapInput("b", "hello")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equal");
            Assert.False(result.GetOutput<bool>("equal", "result"));
        }

        #endregion

        #region Boolean Edge Cases

        [Fact]
        public async Task TestBooleanStringEquality()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("equal", typeof(CoreNodes), "Equal")
                .MapInput("a", "true")
                .MapInput("b", "True")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equal");
            // String comparison should be case-sensitive
            Assert.False(result.GetOutput<bool>("equal", "result"));
        }

        [Fact]
        public async Task TestBooleanAndWithSameValues()
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
        public async Task TestBooleanOrWithBothFalse()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("or", typeof(CoreNodes), "Or")
                .MapInput("a", "false")
                .MapInput("b", "false")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("or");
            Assert.False(result.GetOutput<bool>("or", "result"));
        }

        [Fact]
        public async Task TestNotWithFalse()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("not", typeof(CoreNodes), "Not")
                .MapInput("value", "false")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("not");
            Assert.True(result.GetOutput<bool>("not", "result"));
        }

        [Fact]
        public async Task TestXorWithSameValues()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("xor", typeof(CoreNodes), "Xor")
                .MapInput("a", "true")
                .MapInput("b", "true")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("xor");
            Assert.False(result.GetOutput<bool>("xor", "result"));
        }

        #endregion

        #region Large Numbers and Precision

        [Fact]
        public async Task TestLargeNumberAddition()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "2000000000")
                .MapInput("input2", "147483647")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("add");
            Assert.Equal(2147483647, result.GetOutput<int>("add", "result"));
        }

        [Fact]
        public async Task TestVerySmallDoubleAddition()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "AddD")
                .MapInput("input1", "0.0000001")
                .MapInput("input2", "0.0000002")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("add");
            Assert.Equal(0.0000003, result.GetOutput<double>("add", "result"), 10);
        }

        [Fact]
        public async Task TestFloatingPointPrecisionComparison()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "AddD")
                .MapInput("input1", "0.1")
                .MapInput("input2", "0.2")
                .AutoMapOutputs();

            graph.AddNode("equal", typeof(CoreNodes), "Equal")
                .MapInput("a", "input.result")
                .MapInput("b", "0.3")
                .AutoMapOutputs();

            graph.Connect("add", "equal");

            var result = await graph.ExecuteAsync("add");
            // This might fail due to floating point precision
            var sum = result.GetOutput<double>("add", "result");
            Assert.InRange(sum, 0.299, 0.301);
        }

        [Fact]
        public async Task TestEqualWithToleranceForFloatingPoint()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("equalTol", typeof(CoreNodes), "EqualWithTolerance")
                .MapInput("a", "0.30000001")
                .MapInput("b", "0.3")
                .MapInput("tolerance", "0.0001")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equalTol");
            Assert.True(result.GetOutput<bool>("equalTol", "result"));
        }

        #endregion

        #region Scientific Notation

        [Fact]
        public async Task TestScientificNotationParsing()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "AddD")
                .MapInput("input1", "1e3")
                .MapInput("input2", "2e3")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("add");
            Assert.Equal(3000.0, result.GetOutput<double>("add", "result"));
        }

        [Fact]
        public async Task TestSmallScientificNotation()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("add", typeof(BaseNodeCollection), "AddD")
                .MapInput("input1", "1e-6")
                .MapInput("input2", "2e-6")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("add");
            Assert.Equal(0.000003, result.GetOutput<double>("add", "result"), 10);
        }

        #endregion

        #region Mixed Type Comparisons Through Paths

        [Fact]
        public async Task TestIntegerComparisonWithDoubleResult()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("addDouble", typeof(BaseNodeCollection), "AddD")
                .MapInput("input1", "10.0")
                .MapInput("input2", "5.0")
                .AutoMapOutputs();

            graph.AddNode("equal", typeof(CoreNodes), "Equal")
                .MapInput("a", "input.result")
                .MapInput("b", "15")
                .AutoMapOutputs();

            graph.Connect("addDouble", "equal");

            var result = await graph.ExecuteAsync("addDouble");
            Assert.True(result.GetOutput<bool>("equal", "result"));
        }

        [Fact]
        public async Task TestDoubleComparisonWithIntegerResult()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("addInt", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.AddNode("equal", typeof(CoreNodes), "Equal")
                .MapInput("a", "input.result")
                .MapInput("b", "15.0")
                .AutoMapOutputs();

            graph.Connect("addInt", "equal");

            var result = await graph.ExecuteAsync("addInt");
            Assert.True(result.GetOutput<bool>("equal", "result"));
        }

        [Fact]
        public async Task TestIntegerGreaterThanWithDoubleFromPath()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("multiply", typeof(BaseNodeCollection), "MultiplyD")
                .MapInput("input1", "10.5")
                .MapInput("input2", "2.0")
                .AutoMapOutputs();

            graph.AddNode("greater", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "input.result")
                .MapInput("b", "20")
                .AutoMapOutputs();

            graph.Connect("multiply", "greater");

            var result = await graph.ExecuteAsync("multiply");
            Assert.True(result.GetOutput<bool>("greater", "result")); // 21.0 > 20
        }

        #endregion

        #region String Parsing Edge Cases

        [Fact]
        public async Task TestParseIntFromValidString()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("parse", typeof(BaseNodeCollection), "ParseInt")
                .MapInput("input", "12345")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("parse");
            Assert.Equal(12345, result.GetOutput<int>("parse", "result"));
        }

        [Fact]
        public async Task TestParseDoubleFromInteger()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("parse", typeof(BaseNodeCollection), "ParseDouble")
                .MapInput("input", "42")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("parse");
            Assert.Equal(42.0, result.GetOutput<double>("parse", "result"));
        }

        [Fact]
        public async Task TestParseDoubleFromScientific()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("parse", typeof(BaseNodeCollection), "ParseDouble")
                .MapInput("input", "1.5e10")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("parse");
            Assert.Equal(15000000000.0, result.GetOutput<double>("parse", "result"));
        }

        [Fact]
        public async Task TestParseBoolFromTrue()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("parse", typeof(BaseNodeCollection), "ParseBool")
                .MapInput("input", "True")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("parse");
            Assert.True(result.GetOutput<bool>("parse", "result"));
        }

        [Fact]
        public async Task TestParseBoolFromLowercase()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("parse", typeof(BaseNodeCollection), "ParseBool")
                .MapInput("input", "true")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("parse");
            Assert.True(result.GetOutput<bool>("parse", "result"));
        }

        #endregion

        #region Chained Type Conversions

        [Fact]
        public async Task TestIntToDoubleToIntChain()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("addInt", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.AddNode("sqrt", typeof(BaseNodeCollection), "Sqrt")
                .MapInput("value", "input.result")
                .AutoMapOutputs();

            graph.AddNode("addInt2", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")
                .MapInput("input2", "1")
                .AutoMapOutputs();

            graph.Connect("addInt", "sqrt");
            graph.Connect("sqrt", "addInt2");

            var result = await graph.ExecuteAsync("addInt");

            Assert.Equal(15, result.GetOutput<int>("addInt", "result"));
            Assert.InRange(result.GetOutput<double>("sqrt", "result"), 3.87, 3.88);
            // sqrt(15) ≈ 3.872, converted to int should be 3, 3 + 1 = 4
            Assert.Equal(4, result.GetOutput<int>("addInt2", "result"));
        }

        [Fact]
        public async Task TestBoolToIntConversionChain()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("greater", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "10")
                .MapInput("b", "5")
                .AutoMapOutputs();

            // Pass bool result to int operation - tests conversion
            graph.AddNode("ternary", typeof(CoreNodes), "Ternary")
                .MapInput("condition", "input.result")
                .MapInput("trueValue", "100")
                .MapInput("falseValue", "0")
                .AutoMapOutputs();

            graph.Connect("greater", "ternary");

            var result = await graph.ExecuteAsync("greater");

            Assert.True(result.GetOutput<bool>("greater", "result"));
            Assert.Equal(100, result.GetOutput<int>("ternary", "result"));
        }

        #endregion

        #region Multiple Output Paths

        [Fact]
        public async Task TestSameNodeOutputUsedMultipleTimes()
        {
            // One node output feeds into three different consumers
            var graph = new NodeGraphBuilder();

            graph.AddNode("source", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "20")
                .AutoMapOutputs();

            graph.AddNode("consumer1", typeof(BaseNodeCollection), "Multiply")
                .MapInput("input1", "input.result")
                .MapInput("input2", "2")
                .AutoMapOutputs();

            graph.AddNode("consumer2", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "input.result")
                .MapInput("input2", "5")
                .AutoMapOutputs();

            graph.AddNode("consumer3", typeof(BaseNodeCollection), "Subtract")
                .MapInput("input1", "input.result")
                .MapInput("input2", "10")
                .AutoMapOutputs();

            graph.Connect("source", "consumer1");
            graph.Connect("source", "consumer2");
            graph.Connect("source", "consumer3");

            var result = await graph.ExecuteAsync("source");

            Assert.Equal(30, result.GetOutput<int>("source", "result"));
            Assert.Equal(60, result.GetOutput<int>("consumer1", "result"));  // 30 * 2
            Assert.Equal(35, result.GetOutput<int>("consumer2", "result"));  // 30 + 5
            Assert.Equal(20, result.GetOutput<int>("consumer3", "result"));  // 30 - 10
        }

        #endregion

        #region Comparison Boundary Cases

        [Fact]
        public async Task TestGreaterOrEqualWithEqualValues()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("greaterOrEqual", typeof(CoreNodes), "GreaterOrEqual")
                .MapInput("a", "42")
                .MapInput("b", "42")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("greaterOrEqual");
            Assert.True(result.GetOutput<bool>("greaterOrEqual", "result"));
        }

        [Fact]
        public async Task TestLessOrEqualWithEqualValues()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("lessOrEqual", typeof(CoreNodes), "LessOrEqual")
                .MapInput("a", "42")
                .MapInput("b", "42")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("lessOrEqual");
            Assert.True(result.GetOutput<bool>("lessOrEqual", "result"));
        }

        [Fact]
        public async Task TestNotEqualWithEqualValues()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("notEqual", typeof(CoreNodes), "NotEqual")
                .MapInput("a", "42")
                .MapInput("b", "42")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("notEqual");
            Assert.False(result.GetOutput<bool>("notEqual", "result"));
        }

        #endregion

        #region Hexadecimal and Other Number Formats

        [Fact]
        public async Task TestHexadecimalString()
        {
            var graph = new NodeGraphBuilder();

            // "0xFF" should be treated as string, not parsed as hex
            graph.AddNode("length", typeof(BaseNodeCollection), "Length")
                .MapInput("input", "0xFF")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("length");
            Assert.Equal(4, result.GetOutput<int>("length", "result"));
        }

        #endregion

        #region Case Sensitivity

        [Fact]
        public async Task TestStringEqualsCaseSensitive()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("equal", typeof(CoreNodes), "StringEquals")
                .MapInput("a", "Hello")
                .MapInput("b", "hello")
                .MapInput("ignoreCase", "false")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equal");
            Assert.False(result.GetOutput<bool>("equal", "result"));
        }

        [Fact]
        public async Task TestStringEqualsCaseInsensitive()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("equal", typeof(CoreNodes), "StringEquals")
                .MapInput("a", "Hello")
                .MapInput("b", "hello")
                .MapInput("ignoreCase", "true")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equal");
            Assert.True(result.GetOutput<bool>("equal", "result"));
        }

        #endregion

        #region Decimal/Float Edge Cases

        [Fact]
        public async Task TestDecimalPlaces()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("divide", typeof(BaseNodeCollection), "DivideD")
                .MapInput("numerator", "1")
                .MapInput("denominator", "3")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("divide");
            var quotient = result.GetOutput<double>("divide", "result");
            Assert.InRange(quotient, 0.333, 0.334);
        }

        [Fact]
        public async Task TestTrailingZeroesInDecimal()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("equal", typeof(CoreNodes), "Equal")
                .MapInput("a", "5.0")
                .MapInput("b", "5.00000")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equal");
            Assert.True(result.GetOutput<bool>("equal", "result"));
        }

        #endregion

        #region Ternary Edge Cases

        [Fact]
        public async Task TestTernaryWithTrueCondition()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("ternary", typeof(CoreNodes), "Ternary")
                .MapInput("condition", "true")
                .MapInput("trueValue", "yes")
                .MapInput("falseValue", "no")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("ternary");
            Assert.Equal("yes", result.GetOutput<string>("ternary", "result"));
        }

        [Fact]
        public async Task TestTernaryWithFalseCondition()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("ternary", typeof(CoreNodes), "Ternary")
                .MapInput("condition", "false")
                .MapInput("trueValue", "yes")
                .MapInput("falseValue", "no")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("ternary");
            Assert.Equal("no", result.GetOutput<string>("ternary", "result"));
        }

        [Fact]
        public async Task TestTernaryWithNumericValues()
        {
            var graph = new NodeGraphBuilder();

            graph.AddNode("ternary", typeof(CoreNodes), "Ternary")
                .MapInput("condition", "true")
                .MapInput("trueValue", "100")
                .MapInput("falseValue", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("ternary");
            Assert.Equal(100, result.GetOutput<int>("ternary", "result"));
        }

        #endregion
    }
}
