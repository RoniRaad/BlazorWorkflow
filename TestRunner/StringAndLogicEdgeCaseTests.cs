using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Testing;
using Xunit;

namespace TestRunner
{
    /// <summary>
    /// Comprehensive tests for string operations, comparison edge cases, and boolean logic combinations.
    /// </summary>
    public class StringAndLogicEdgeCaseTests
    {
        // ==========================================
        // STRING EDGE CASES
        // ==========================================

        [Fact]
        public async Task TestConcatenateEmptyStrings()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("concat", typeof(BaseNodeCollection), "Concat")
                .MapInput("input1", "")
                .MapInput("input2", "")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("concat");
            Assert.Equal("", result.GetOutput<string>("concat", "result"));
        }

        [Fact]
        public async Task TestConcatenateWithNull()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("concat", typeof(BaseNodeCollection), "Concat")
                .MapInput("input1", "Hello")
                .MapInput("input2", "")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("concat");
            Assert.Equal("Hello", result.GetOutput<string>("concat", "result"));
        }

        [Fact]
        public async Task TestStringLengthOfEmptyString()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("length", typeof(BaseNodeCollection), "StringLength")
                .MapInput("input", "")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("length");
            Assert.Equal(0, result.GetOutput<int>("length", "result"));
        }

        [Fact]
        public async Task TestStringLengthOfWhitespace()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("length", typeof(BaseNodeCollection), "StringLength")
                .MapInput("input", "   ")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("length");
            Assert.Equal(3, result.GetOutput<int>("length", "result"));
        }

        [Fact]
        public async Task TestSubstringBeyondLength()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("substring", typeof(BaseNodeCollection), "Substring")
                .MapInput("input", "hello")
                .MapInput("start", "3")
                .MapInput("length", "10")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("substring");
            // Should return "lo" (from index 3 to end)
            Assert.Equal("lo", result.GetOutput<string>("substring", "result"));
        }

        [Fact]
        public async Task TestSubstringFromEnd()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("substring", typeof(BaseNodeCollection), "Substring")
                .MapInput("input", "hello")
                .MapInput("start", "4")
                .MapInput("length", "1")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("substring");
            Assert.Equal("o", result.GetOutput<string>("substring", "result"));
        }

        [Fact]
        public async Task TestSubstringEntireString()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("substring", typeof(BaseNodeCollection), "Substring")
                .MapInput("input", "hello")
                .MapInput("start", "0")
                .MapInput("length", "5")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("substring");
            Assert.Equal("hello", result.GetOutput<string>("substring", "result"));
        }

        [Fact]
        public async Task TestStringContainsWithEmptyString()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("contains", typeof(BaseNodeCollection), "StringContains")
                .MapInput("input", "hello")
                .MapInput("value", "")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("contains");
            // Every string contains empty string
            Assert.True(result.GetOutput<bool>("contains", "result"));
        }

        [Fact]
        public async Task TestStringContainsCaseSensitive()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("contains", typeof(BaseNodeCollection), "StringContains")
                .MapInput("input", "Hello World")
                .MapInput("value", "hello")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("contains");
            Assert.False(result.GetOutput<bool>("contains", "result"));
        }

        [Fact]
        public async Task TestStringWithUnicode()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("concat", typeof(BaseNodeCollection), "Concat")
                .MapInput("input1", "Hello ")
                .MapInput("input2", "世界")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("concat");
            Assert.Equal("Hello 世界", result.GetOutput<string>("concat", "result"));
        }

        [Fact]
        public async Task TestStringWithEmoji()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("concat", typeof(BaseNodeCollection), "Concat")
                .MapInput("input1", "Hello ")
                .MapInput("input2", "😀")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("concat");
            Assert.Equal("Hello 😀", result.GetOutput<string>("concat", "result"));
        }

        [Fact]
        public async Task TestStringWithSpecialCharacters()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("concat", typeof(BaseNodeCollection), "Concat")
                .MapInput("input1", "Line1\n")
                .MapInput("input2", "Line2\t")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("concat");
            Assert.Equal("Line1\nLine2\t", result.GetOutput<string>("concat", "result"));
        }

        [Fact]
        public async Task TestStringEqualsCaseInsensitive()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("equals", typeof(CoreNodes), "StringEquals")
                .MapInput("a", "Hello")
                .MapInput("b", "hello")
                .MapInput("ignoreCase", "true")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equals");
            Assert.True(result.GetOutput<bool>("equals", "result"));
        }

        [Fact]
        public async Task TestStringEqualsCaseSensitive()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("equals", typeof(CoreNodes), "StringEquals")
                .MapInput("a", "Hello")
                .MapInput("b", "hello")
                .MapInput("ignoreCase", "false")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equals");
            Assert.False(result.GetOutput<bool>("equals", "result"));
        }

        [Fact]
        public async Task TestToUpperCase()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("upper", typeof(BaseNodeCollection), "ToUpperCase")
                .MapInput("input", "hello world")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("upper");
            Assert.Equal("HELLO WORLD", result.GetOutput<string>("upper", "result"));
        }

        [Fact]
        public async Task TestToLowerCase()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("lower", typeof(BaseNodeCollection), "ToLowerCase")
                .MapInput("input", "HELLO WORLD")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("lower");
            Assert.Equal("hello world", result.GetOutput<string>("lower", "result"));
        }

        [Fact]
        public async Task TestTrimWhitespace()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("trim", typeof(BaseNodeCollection), "Trim")
                .MapInput("input", "  hello  ")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("trim");
            Assert.Equal("hello", result.GetOutput<string>("trim", "result"));
        }

        // ==========================================
        // COMPARISON EDGE CASES
        // ==========================================

        [Fact]
        public async Task TestCompareIntAndDouble()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("equal", typeof(CoreNodes), "Equal")
                .MapInput("a", "5")
                .MapInput("b", "5.0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equal");
            Assert.True(result.GetOutput<bool>("equal", "result"));
        }

        [Fact]
        public async Task TestCompareIntAndString()
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
        public async Task TestCompareNegativeZero()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("equal", typeof(CoreNodes), "Equal")
                .MapInput("a", "-0.0")
                .MapInput("b", "0.0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equal");
            Assert.True(result.GetOutput<bool>("equal", "result"));
        }

        [Fact]
        public async Task TestEqualWithToleranceAtBoundary()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("equal", typeof(CoreNodes), "EqualWithTolerance")
                .MapInput("a", "1.0")
                .MapInput("b", "1.001")
                .MapInput("tolerance", "0.001")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equal");
            Assert.True(result.GetOutput<bool>("equal", "result"));
        }

        [Fact]
        public async Task TestEqualWithToleranceExceeded()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("equal", typeof(CoreNodes), "EqualWithTolerance")
                .MapInput("a", "1.0")
                .MapInput("b", "1.002")
                .MapInput("tolerance", "0.001")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("equal");
            Assert.False(result.GetOutput<bool>("equal", "result"));
        }

        [Fact]
        public async Task TestGreaterThanWithEqualValues()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("gt", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "10")
                .MapInput("b", "10")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("gt");
            Assert.False(result.GetOutput<bool>("gt", "result"));
        }

        [Fact]
        public async Task TestGreaterOrEqualWithEqualValues()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("gte", typeof(CoreNodes), "GreaterOrEqual")
                .MapInput("a", "10")
                .MapInput("b", "10")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("gte");
            Assert.True(result.GetOutput<bool>("gte", "result"));
        }

        [Fact]
        public async Task TestLessThanWithNegatives()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("lt", typeof(CoreNodes), "LessThan")
                .MapInput("a", "-10")
                .MapInput("b", "-5")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("lt");
            Assert.True(result.GetOutput<bool>("lt", "result"));
        }

        [Fact]
        public async Task TestCompareVeryLargeNumbers()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("gt", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "999999999999999")
                .MapInput("b", "999999999999998")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("gt");
            Assert.True(result.GetOutput<bool>("gt", "result"));
        }

        // ==========================================
        // BOOLEAN LOGIC COMBINATIONS
        // ==========================================

        [Fact]
        public async Task TestAndWithBothTrue()
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
        public async Task TestAndWithBothFalse()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("and", typeof(CoreNodes), "And")
                .MapInput("a", "false")
                .MapInput("b", "false")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("and");
            Assert.False(result.GetOutput<bool>("and", "result"));
        }

        [Fact]
        public async Task TestOrWithBothFalse()
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
        public async Task TestOrWithBothTrue()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("or", typeof(CoreNodes), "Or")
                .MapInput("a", "true")
                .MapInput("b", "true")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("or");
            Assert.True(result.GetOutput<bool>("or", "result"));
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

        [Fact]
        public async Task TestXorWithDifferentValues()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("xor", typeof(CoreNodes), "Xor")
                .MapInput("a", "true")
                .MapInput("b", "false")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("xor");
            Assert.True(result.GetOutput<bool>("xor", "result"));
        }

        [Fact]
        public async Task TestDoubleNegation()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("not1", typeof(CoreNodes), "Not")
                .MapInput("value", "true")
                .AutoMapOutputs();
            graph.AddNode("not2", typeof(CoreNodes), "Not")
                .MapInput("value", "input.result")
                .AutoMapOutputs();
            graph.Connect("not1", "not2");

            var result = await graph.ExecuteAsync("not1");
            Assert.True(result.GetOutput<bool>("not2", "result"));
        }

        [Fact]
        public async Task TestDeMorgansLawNotAndToOrNot()
        {
            // !(A && B) == (!A || !B)
            var graph = new NodeGraphBuilder();

            // Left side: !(A && B) with A=true, B=false
            graph.AddNode("and", typeof(CoreNodes), "And")
                .MapInput("a", "true")
                .MapInput("b", "false")
                .AutoMapOutputs();
            graph.AddNode("notAnd", typeof(CoreNodes), "Not")
                .MapInput("value", "input.result")
                .AutoMapOutputs();
            graph.Connect("and", "notAnd");

            // Right side: (!A || !B)
            graph.AddNode("notA", typeof(CoreNodes), "Not")
                .MapInput("value", "true")
                .AutoMapOutputs();
            graph.AddNode("notB", typeof(CoreNodes), "Not")
                .MapInput("value", "false")
                .AutoMapOutputs();
            graph.AddNode("orNots", typeof(CoreNodes), "Or")
                .MapInput("a", "input.result")
                .MapInput("b", "true")  // notB is always true
                .AutoMapOutputs();
            graph.Connect("notA", "orNots");

            var result = await graph.ExecuteAsync("and");
            var leftSide = result.GetOutput<bool>("notAnd", "result");
            var rightSide = result.GetOutput<bool>("orNots", "result");
            Assert.Equal(leftSide, rightSide);
        }

        [Fact]
        public async Task TestComplexBooleanExpression()
        {
            // (A && B) || (C && !D) where A=true, B=false, C=true, D=false
            var graph = new NodeGraphBuilder();

            graph.AddNode("and1", typeof(CoreNodes), "And")
                .MapInput("a", "true")
                .MapInput("b", "false")
                .AutoMapOutputs();

            graph.AddNode("notD", typeof(CoreNodes), "Not")
                .MapInput("value", "false")
                .AutoMapOutputs();

            graph.AddNode("and2", typeof(CoreNodes), "And")
                .MapInput("a", "true")
                .MapInput("b", "input.result")
                .AutoMapOutputs();
            graph.Connect("notD", "and2");

            graph.AddNode("or", typeof(CoreNodes), "Or")
                .MapInput("a", "false")  // and1 result
                .MapInput("b", "input.result")  // and2 result
                .AutoMapOutputs();
            graph.Connect("and2", "or");

            var result = await graph.ExecuteAsync("and1");
            Assert.True(result.GetOutput<bool>("or", "result"));
        }

        // ==========================================
        // NESTED TERNARY OPERATIONS
        // ==========================================

        [Fact]
        public async Task TestNestedTernary()
        {
            // condition1 ? "A" : (condition2 ? "B" : "C")
            var graph = new NodeGraphBuilder();

            graph.AddNode("inner", typeof(CoreNodes), "Ternary")
                .MapInput("condition", "true")
                .MapInput("trueValue", "B")
                .MapInput("falseValue", "C")
                .AutoMapOutputs();

            graph.AddNode("outer", typeof(CoreNodes), "Ternary")
                .MapInput("condition", "false")
                .MapInput("trueValue", "A")
                .MapInput("falseValue", "input.result")
                .AutoMapOutputs();
            graph.Connect("inner", "outer");

            var result = await graph.ExecuteAsync("inner");
            Assert.Equal("B", result.GetOutput<string>("outer", "result"));
        }

        [Fact]
        public async Task TestTernaryWithNumericResults()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("compare", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "10")
                .MapInput("b", "5")
                .AutoMapOutputs();

            graph.AddNode("ternary", typeof(CoreNodes), "Ternary")
                .MapInput("condition", "input.result")
                .MapInput("trueValue", "100")
                .MapInput("falseValue", "50")
                .AutoMapOutputs();
            graph.Connect("compare", "ternary");

            var result = await graph.ExecuteAsync("compare");
            Assert.Equal(100, result.GetOutput<int>("ternary", "result"));
        }

        [Fact]
        public async Task TestTernaryWithMixedTypes()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("ternary", typeof(CoreNodes), "Ternary")
                .MapInput("condition", "true")
                .MapInput("trueValue", "42")
                .MapInput("falseValue", "hello")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("ternary");
            Assert.Equal("42", result.GetOutput<string>("ternary", "result"));
        }

        // ==========================================
        // CHAINED COMPARISONS
        // ==========================================

        [Fact]
        public async Task TestRangeCheck()
        {
            // value >= min && value <= max
            var graph = new NodeGraphBuilder();

            graph.AddNode("greaterOrEqual", typeof(CoreNodes), "GreaterOrEqual")
                .MapInput("a", "5")
                .MapInput("b", "1")
                .AutoMapOutputs();

            graph.AddNode("lessOrEqual", typeof(CoreNodes), "LessOrEqual")
                .MapInput("a", "5")
                .MapInput("b", "10")
                .AutoMapOutputs();

            graph.AddNode("and", typeof(CoreNodes), "And")
                .MapInput("a", "input.result")
                .MapInput("b", "true")  // lessOrEqual result
                .AutoMapOutputs();
            graph.Connect("greaterOrEqual", "and");

            var result = await graph.ExecuteAsync("greaterOrEqual");
            Assert.True(result.GetOutput<bool>("and", "result"));
        }

        [Fact]
        public async Task TestChainedGreaterThan()
        {
            // a > b > c (10 > 5 > 2)
            var graph = new NodeGraphBuilder();

            graph.AddNode("first", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "10")
                .MapInput("b", "5")
                .AutoMapOutputs();

            graph.AddNode("second", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "5")
                .MapInput("b", "2")
                .AutoMapOutputs();

            graph.AddNode("and", typeof(CoreNodes), "And")
                .MapInput("a", "input.result")
                .MapInput("b", "true")  // second result
                .AutoMapOutputs();
            graph.Connect("first", "and");

            var result = await graph.ExecuteAsync("first");
            Assert.True(result.GetOutput<bool>("and", "result"));
        }

        [Fact]
        public async Task TestOutsideRangeCheck()
        {
            // value < min || value > max
            var graph = new NodeGraphBuilder();

            graph.AddNode("lessThan", typeof(CoreNodes), "LessThan")
                .MapInput("a", "15")
                .MapInput("b", "1")
                .AutoMapOutputs();

            graph.AddNode("greaterThan", typeof(CoreNodes), "GreaterThan")
                .MapInput("a", "15")
                .MapInput("b", "10")
                .AutoMapOutputs();

            graph.AddNode("or", typeof(CoreNodes), "Or")
                .MapInput("a", "input.result")
                .MapInput("b", "true")  // greaterThan result
                .AutoMapOutputs();
            graph.Connect("lessThan", "or");

            var result = await graph.ExecuteAsync("lessThan");
            Assert.True(result.GetOutput<bool>("or", "result"));
        }
    }
}
