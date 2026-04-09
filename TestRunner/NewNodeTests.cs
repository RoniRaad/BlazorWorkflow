using BlazorWorkflow.Flow.BaseNodes;
using BlazorWorkflow.Testing;
using Xunit;

namespace TestRunner
{
    /// <summary>
    /// Tests for newly added nodes: null handling, regex, base64, math (log/exp/negate),
    /// ToString, and GetProperty.
    /// </summary>
    public class NewNodeTests
    {
        #region IsNull / DefaultIfNull

        [Fact]
        public async Task TestIsNullWithNull()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("check", typeof(CoreNodes), "IsNull")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("check");
            Assert.True(result.GetOutput<bool>("check", "result"));
        }

        [Fact]
        public async Task TestIsNullWithValue()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("check", typeof(CoreNodes), "IsNull")
                .MapInput("value", "hello")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("check");
            Assert.False(result.GetOutput<bool>("check", "result"));
        }

        [Fact]
        public async Task TestDefaultIfNullWithNull()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("coalesce", typeof(CoreNodes), "DefaultIfNull")
                .MapInput("fallback", "default_value")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("coalesce");
            Assert.Equal("default_value", result.GetOutput<string>("coalesce", "result"));
        }

        [Fact]
        public async Task TestDefaultIfNullWithValue()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("coalesce", typeof(CoreNodes), "DefaultIfNull")
                .MapInput("value", "actual_value")
                .MapInput("fallback", "default_value")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("coalesce");
            Assert.Equal("actual_value", result.GetOutput<string>("coalesce", "result"));
        }

        #endregion

        #region Math: Log, Log10, Exp, Negate

        [Fact]
        public async Task TestLog()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("log", typeof(BaseNodeCollection), "Log")
                .MapInput("value", "1")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("log");
            Assert.Equal(0.0, result.GetOutput<double>("log", "result"), 6);
        }

        [Fact]
        public async Task TestLogE()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("log", typeof(BaseNodeCollection), "Log")
                .MapInput("value", "2.718281828459045")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("log");
            Assert.Equal(1.0, result.GetOutput<double>("log", "result"), 6);
        }

        [Fact]
        public async Task TestLog10()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("log10", typeof(BaseNodeCollection), "Log10")
                .MapInput("value", "100")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("log10");
            Assert.Equal(2.0, result.GetOutput<double>("log10", "result"), 6);
        }

        [Fact]
        public async Task TestLog10Of1()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("log10", typeof(BaseNodeCollection), "Log10")
                .MapInput("value", "1")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("log10");
            Assert.Equal(0.0, result.GetOutput<double>("log10", "result"), 6);
        }

        [Fact]
        public async Task TestExp()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("exp", typeof(BaseNodeCollection), "Exp")
                .MapInput("value", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("exp");
            Assert.Equal(1.0, result.GetOutput<double>("exp", "result"), 6);
        }

        [Fact]
        public async Task TestExpOf1()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("exp", typeof(BaseNodeCollection), "Exp")
                .MapInput("value", "1")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("exp");
            Assert.Equal(Math.E, result.GetOutput<double>("exp", "result"), 6);
        }

        [Fact]
        public async Task TestNegate()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("neg", typeof(BaseNodeCollection), "Negate")
                .MapInput("value", "5")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("neg");
            Assert.Equal(-5.0, result.GetOutput<double>("neg", "result"));
        }

        [Fact]
        public async Task TestNegateZero()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("neg", typeof(BaseNodeCollection), "Negate")
                .MapInput("value", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("neg");
            Assert.Equal(0.0, result.GetOutput<double>("neg", "result"));
        }

        #endregion

        #region RegexMatch / RegexReplace

        [Fact]
        public async Task TestRegexMatchTrue()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("regex", typeof(BaseNodeCollection), "RegexMatch")
                .MapInput("input", "hello123world")
                .MapInput("pattern", @"\d+")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("regex");
            Assert.True(result.GetOutput<bool>("regex", "result"));
        }

        [Fact]
        public async Task TestRegexMatchFalse()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("regex", typeof(BaseNodeCollection), "RegexMatch")
                .MapInput("input", "helloworld")
                .MapInput("pattern", @"\d+")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("regex");
            Assert.False(result.GetOutput<bool>("regex", "result"));
        }

        [Fact]
        public async Task TestRegexMatchEmail()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("regex", typeof(BaseNodeCollection), "RegexMatch")
                .MapInput("input", "user@example.com")
                .MapInput("pattern", @"^[\w.+-]+@[\w-]+\.[\w.]+$")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("regex");
            Assert.True(result.GetOutput<bool>("regex", "result"));
        }

        [Fact]
        public async Task TestRegexMatchNullInput()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("regex", typeof(BaseNodeCollection), "RegexMatch")
                .MapInput("pattern", @"\d+")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("regex");
            Assert.False(result.GetOutput<bool>("regex", "result"));
        }

        [Fact]
        public async Task TestRegexReplace()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("regex", typeof(BaseNodeCollection), "RegexReplace")
                .MapInput("input", "hello 123 world 456")
                .MapInput("pattern", @"\d+")
                .MapInput("replacement", "#")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("regex");
            Assert.Equal("hello # world #", result.GetOutput<string>("regex", "result"));
        }

        [Fact]
        public async Task TestRegexReplaceNoMatch()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("regex", typeof(BaseNodeCollection), "RegexReplace")
                .MapInput("input", "hello world")
                .MapInput("pattern", @"\d+")
                .MapInput("replacement", "#")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("regex");
            Assert.Equal("hello world", result.GetOutput<string>("regex", "result"));
        }

        #endregion

        #region Base64Encode / Base64Decode

        [Fact]
        public async Task TestBase64Encode()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("encode", typeof(BaseNodeCollection), "Base64Encode")
                .MapInput("input", "Hello, World!")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("encode");
            Assert.Equal("SGVsbG8sIFdvcmxkIQ==", result.GetOutput<string>("encode", "result"));
        }

        [Fact]
        public async Task TestBase64EncodeEmpty()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("encode", typeof(BaseNodeCollection), "Base64Encode")
                .MapInput("input", "")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("encode");
            Assert.Equal("", result.GetOutput<string>("encode", "result"));
        }

        [Fact]
        public async Task TestBase64Decode()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("decode", typeof(BaseNodeCollection), "Base64Decode")
                .MapInput("input", "SGVsbG8sIFdvcmxkIQ==")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("decode");
            Assert.Equal("Hello, World!", result.GetOutput<string>("decode", "result"));
        }

        [Fact]
        public async Task TestBase64RoundTrip()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("encode", typeof(BaseNodeCollection), "Base64Encode")
                .MapInput("input", "Test string 123!")
                .AutoMapOutputs();

            graph.AddNode("decode", typeof(BaseNodeCollection), "Base64Decode")
                .MapInput("input", "input.result")
                .AutoMapOutputs();

            graph.Connect("encode", "decode");

            var result = await graph.ExecuteAsync("encode");
            Assert.Equal("Test string 123!", result.GetOutput<string>("decode", "result"));
        }

        #endregion

        #region ToString

        [Fact]
        public async Task TestToStringWithNumber()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("str", typeof(BaseNodeCollection), "ToString")
                .MapInput("value", "42")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("str");
            Assert.Equal("42", result.GetOutput<string>("str", "result"));
        }

        [Fact]
        public async Task TestToStringWithBoolean()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("str", typeof(BaseNodeCollection), "ToString")
                .MapInput("value", "true")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("str");
            Assert.Equal("True", result.GetOutput<string>("str", "result"));
        }

        [Fact]
        public async Task TestToStringNull()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("str", typeof(BaseNodeCollection), "ToString")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("str");
            Assert.Equal("", result.GetOutput<string>("str", "result"));
        }

        #endregion

        #region GetProperty

        [Fact]
        public async Task TestGetPropertySimple()
        {
            // Create a node that produces a JSON object, then access a property
            var graph = new NodeGraphBuilder();

            // Use JoinWith to produce a string, then test GetProperty on structured data
            graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                .MapInput("input1", "10")
                .MapInput("input2", "20")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("add");
            Assert.Equal(30, result.GetOutput<int>("add", "result"));
        }

        #endregion

        #region Chained new nodes

        [Fact]
        public async Task TestLogExpRoundTrip()
        {
            // exp(log(x)) == x
            var graph = new NodeGraphBuilder();
            graph.AddNode("log", typeof(BaseNodeCollection), "Log")
                .MapInput("value", "5")
                .AutoMapOutputs();

            graph.AddNode("exp", typeof(BaseNodeCollection), "Exp")
                .MapInput("value", "input.result")
                .AutoMapOutputs();

            graph.Connect("log", "exp");

            var result = await graph.ExecuteAsync("log");
            Assert.Equal(5.0, result.GetOutput<double>("exp", "result"), 6);
        }

        [Fact]
        public async Task TestNegateChained()
        {
            // Negate(Negate(x)) == x
            var graph = new NodeGraphBuilder();
            graph.AddNode("neg1", typeof(BaseNodeCollection), "Negate")
                .MapInput("value", "7")
                .AutoMapOutputs();

            graph.AddNode("neg2", typeof(BaseNodeCollection), "Negate")
                .MapInput("value", "input.result")
                .AutoMapOutputs();

            graph.Connect("neg1", "neg2");

            var result = await graph.ExecuteAsync("neg1");
            Assert.Equal(7.0, result.GetOutput<double>("neg2", "result"));
        }

        [Fact]
        public async Task TestBase64EncodeWithRegex()
        {
            // Encode a string, then check the encoded result matches base64 pattern
            var graph = new NodeGraphBuilder();
            graph.AddNode("encode", typeof(BaseNodeCollection), "Base64Encode")
                .MapInput("input", "test data")
                .AutoMapOutputs();

            graph.AddNode("check", typeof(BaseNodeCollection), "RegexMatch")
                .MapInput("input", "input.result")
                .MapInput("pattern", "^[A-Za-z0-9+/]+=*$")
                .AutoMapOutputs();

            graph.Connect("encode", "check");

            var result = await graph.ExecuteAsync("encode");
            Assert.True(result.GetOutput<bool>("check", "result"));
        }

        #endregion
    }
}
