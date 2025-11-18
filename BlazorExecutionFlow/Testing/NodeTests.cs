using System.Text.Json.Nodes;
using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models.NodeV2;

namespace BlazorExecutionFlow.Testing
{
    /// <summary>
    /// Comprehensive tests for all BaseNodeCollection nodes.
    /// Tests edge cases, error handling, and correctness.
    /// </summary>
    public static class NodeTests
    {
        private static readonly List<string> FailedTests = new();
        private static readonly List<string> PassedTests = new();

        public static async Task RunAllTests()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════╗");
            Console.WriteLine("║   Node Comprehensive Test Suite                  ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════╝\n");

            await TestMathNodes();
            await TestComparisonNodes();
            await TestStringNodes();
            await TestLogicNodes();
            await TestParsingNodes();
            await TestDateTimeNodes();
            await TestCollectionNodes();
            await TestJsonNodes();
            await TestControlFlowNodes();
            await TestVariableNodes();
            await TestRandomNodes();

            // New utility nodes
            await TestNewStringUtilities();
            await TestNewArrayUtilities();
            await TestNewLogicUtilities();
            await TestNewMathUtilities();
            await TestNewDateTimeUtilities();

            // Serialization tests
            await TestFlowSerialization();

            PrintSummary();
        }

        private static async Task TestMathNodes()
        {
            Console.WriteLine("\n[Math Nodes]");

            // Add
            await Test("Add: Basic addition", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                    .MapInput("input1", "5")
                    .MapInput("input2", "10")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("add");
                return result.GetOutput<int>("add", "result") == 15;
            });

            // Add with negative numbers
            await Test("Add: Negative numbers", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                    .MapInput("input1", "-5")
                    .MapInput("input2", "10")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("add");
                return result.GetOutput<int>("add", "result") == 5;
            });

            // Subtract
            await Test("Subtract: Basic subtraction", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("sub", typeof(BaseNodeCollection), "Subtract")
                    .MapInput("input1", "10")
                    .MapInput("input2", "3")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("sub");
                return result.GetOutput<int>("sub", "result") == 7;
            });

            // Multiply
            await Test("Multiply: Basic multiplication", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("mul", typeof(BaseNodeCollection), "Multiply")
                    .MapInput("input1", "6")
                    .MapInput("input2", "7")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("mul");
                return result.GetOutput<int>("mul", "result") == 42;
            });

            // Divide
            await Test("Divide: Basic division", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("div", typeof(BaseNodeCollection), "Divide")
                    .MapInput("numerator", "20")
                    .MapInput("denominator", "4")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("div");
                return result.GetOutput<int>("div", "result") == 5;
            });

            // Divide by zero - should throw
            await Test("Divide: Division by zero throws exception", async () =>
            {
                try
                {
                    var graph = new NodeGraphBuilder();
                    graph.AddNode("div", typeof(BaseNodeCollection), "Divide")
                        .MapInput("numerator", "10")
                        .MapInput("denominator", "0")
                        .AutoMapOutputs();

                    await graph.ExecuteAsync("div");
                    return false; // Should have thrown
                }
                catch (Exception ex)
                {
                    // Check if it's DivideByZeroException or wrapped in TargetInvocationException
                    if (ex is DivideByZeroException)
                        return true;
                    if (ex.InnerException is DivideByZeroException)
                        return true;
                    throw; // Unexpected exception
                }
            });

            // Modulo
            await Test("Modulo: Basic modulo", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("mod", typeof(BaseNodeCollection), "Modulo")
                    .MapInput("input1", "10")
                    .MapInput("input2", "3")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("mod");
                return result.GetOutput<int>("mod", "result") == 1;
            });

            // Min/Max
            await Test("Min: Returns smaller value", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("min", typeof(BaseNodeCollection), "Min")
                    .MapInput("a", "5")
                    .MapInput("b", "10")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("min");
                return result.GetOutput<int>("min", "result") == 5;
            });

            await Test("Max: Returns larger value", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("max", typeof(BaseNodeCollection), "Max")
                    .MapInput("a", "5")
                    .MapInput("b", "10")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("max");
                return result.GetOutput<int>("max", "result") == 10;
            });

            // Clamp
            await Test("Clamp: Value within range", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("clamp", typeof(BaseNodeCollection), "Clamp")
                    .MapInput("value", "5")
                    .MapInput("min", "0")
                    .MapInput("max", "10")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("clamp");
                return result.GetOutput<int>("clamp", "result") == 5;
            });

            await Test("Clamp: Value below range", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("clamp", typeof(BaseNodeCollection), "Clamp")
                    .MapInput("value", "-5")
                    .MapInput("min", "0")
                    .MapInput("max", "10")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("clamp");
                return result.GetOutput<int>("clamp", "result") == 0;
            });

            await Test("Clamp: Value above range", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("clamp", typeof(BaseNodeCollection), "Clamp")
                    .MapInput("value", "15")
                    .MapInput("min", "0")
                    .MapInput("max", "10")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("clamp");
                return result.GetOutput<int>("clamp", "result") == 10;
            });

            // Abs, Negate, Sign
            await Test("Abs: Absolute value of negative", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("abs", typeof(BaseNodeCollection), "Abs")
                    .MapInput("value", "-42")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("abs");
                return result.GetOutput<int>("abs", "result") == 42;
            });

            await Test("Negate: Negates positive", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("neg", typeof(BaseNodeCollection), "Negate")
                    .MapInput("value", "42")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("neg");
                return result.GetOutput<int>("neg", "result") == -42;
            });

            await Test("Sign: Positive number", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("sign", typeof(BaseNodeCollection), "Sign")
                    .MapInput("value", "42")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("sign");
                return result.GetOutput<int>("sign", "result") == 1;
            });

            await Test("Sign: Negative number", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("sign", typeof(BaseNodeCollection), "Sign")
                    .MapInput("value", "-42")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("sign");
                return result.GetOutput<int>("sign", "result") == -1;
            });

            await Test("Sign: Zero", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("sign", typeof(BaseNodeCollection), "Sign")
                    .MapInput("value", "0")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("sign");
                return result.GetOutput<int>("sign", "result") == 0;
            });

            // Increment/Decrement
            await Test("Increment: Adds 1", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("inc", typeof(BaseNodeCollection), "Increment")
                    .MapInput("value", "5")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("inc");
                return result.GetOutput<int>("inc", "result") == 6;
            });

            await Test("Decrement: Subtracts 1", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("dec", typeof(BaseNodeCollection), "Decrement")
                    .MapInput("value", "5")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("dec");
                return result.GetOutput<int>("dec", "result") == 4;
            });

            // MapRange
            await Test("MapRange: Maps value to new range", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("map", typeof(BaseNodeCollection), "MapRange")
                    .MapInput("value", "5")
                    .MapInput("inMin", "0")
                    .MapInput("inMax", "10")
                    .MapInput("outMin", "0")
                    .MapInput("outMax", "100")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("map");
                return result.GetOutput<double>("map", "result") == 50.0;
            });

            // Double operations
            await Test("AddD: Double addition", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("addD", typeof(BaseNodeCollection), "AddD")
                    .MapInput("input1", "5.5")
                    .MapInput("input2", "10.5")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("addD");
                return result.GetOutput<double>("addD", "result") == 16.0;
            });

            await Test("Sqrt: Square root", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("sqrt", typeof(BaseNodeCollection), "Sqrt")
                    .MapInput("value", "16")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("sqrt");
                return result.GetOutput<double>("sqrt", "result") == 4.0;
            });

            await Test("Pow: Power calculation", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("pow", typeof(BaseNodeCollection), "Pow")
                    .MapInput("base", "2")
                    .MapInput("exponent", "3")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("pow");
                return result.GetOutput<double>("pow", "result") == 8.0;
            });

            await Test("RoundD: Rounds to 2 decimals", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("round", typeof(BaseNodeCollection), "RoundD")
                    .MapInput("value", "3.14159")
                    .MapInput("digits", "2")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("round");
                return result.GetOutput<double>("round", "result") == 3.14;
            });

            await Test("FloorD: Floors value", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("floor", typeof(BaseNodeCollection), "FloorD")
                    .MapInput("value", "3.9")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("floor");
                return result.GetOutput<double>("floor", "result") == 3.0;
            });

            await Test("CeilingD: Ceiling value", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("ceil", typeof(BaseNodeCollection), "CeilingD")
                    .MapInput("value", "3.1")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("ceil");
                return result.GetOutput<double>("ceil", "result") == 4.0;
            });

            await Test("Lerp: Linear interpolation", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("lerp", typeof(BaseNodeCollection), "Lerp")
                    .MapInput("a", "0")
                    .MapInput("b", "100")
                    .MapInput("t", "0.5")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("lerp");
                return result.GetOutput<double>("lerp", "result") == 50.0;
            });
        }

        private static async Task TestComparisonNodes()
        {
            Console.WriteLine("\n[Comparison Nodes]");

            await Test("Equal: Equal values", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("eq", typeof(BaseNodeCollection), "Equal")
                    .MapInput("a", "5")
                    .MapInput("b", "5")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("eq");
                return result.GetOutput<bool>("eq", "result") == true;
            });

            await Test("Equal: Unequal values", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("eq", typeof(BaseNodeCollection), "Equal")
                    .MapInput("a", "5")
                    .MapInput("b", "10")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("eq");
                return result.GetOutput<bool>("eq", "result") == false;
            });

            await Test("GreaterThan: True case", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("gt", typeof(BaseNodeCollection), "GreaterThan")
                    .MapInput("a", "10")
                    .MapInput("b", "5")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("gt");
                return result.GetOutput<bool>("gt", "result") == true;
            });

            await Test("LessThan: True case", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("lt", typeof(BaseNodeCollection), "LessThan")
                    .MapInput("a", "5")
                    .MapInput("b", "10")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("lt");
                return result.GetOutput<bool>("lt", "result") == true;
            });

            await Test("EqualD: Within tolerance", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("eqD", typeof(BaseNodeCollection), "EqualD")
                    .MapInput("a", "1.001")
                    .MapInput("b", "1.002")
                    .MapInput("tolerance", "0.01")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("eqD");
                return result.GetOutput<bool>("eqD", "result") == true;
            });

            await Test("StringEquals: Case sensitive", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("strEq", typeof(BaseNodeCollection), "StringEquals")
                    .MapInput("a", "\"Hello\"")
                    .MapInput("b", "\"Hello\"")
                    .MapInput("ignoreCase", "false")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("strEq");
                return result.GetOutput<bool>("strEq", "result") == true;
            });

            await Test("StringEquals: Case insensitive", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("strEq", typeof(BaseNodeCollection), "StringEquals")
                    .MapInput("a", "\"Hello\"")
                    .MapInput("b", "\"hello\"")
                    .MapInput("ignoreCase", "true")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("strEq");
                return result.GetOutput<bool>("strEq", "result") == true;
            });
        }

        private static async Task TestStringNodes()
        {
            Console.WriteLine("\n[String Nodes]");

            await Test("StringConcat: Concatenates two strings", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("concat", typeof(BaseNodeCollection), "StringConcat")
                    .MapInput("input1", "\"Hello\"")
                    .MapInput("input2", "\"World\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("concat");
                return result.GetOutput<string>("concat", "result") == "HelloWorld";
            });

            await Test("JoinWith: Joins with separator", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("join", typeof(BaseNodeCollection), "JoinWith")
                    .MapInput("input1", "\"Hello\"")
                    .MapInput("input2", "\"World\"")
                    .MapInput("separator", "\" \"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("join");
                return result.GetOutput<string>("join", "result") == "Hello World";
            });

            await Test("ToUpper: Converts to uppercase", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("upper", typeof(BaseNodeCollection), "ToUpper")
                    .MapInput("input", "\"hello\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("upper");
                return result.GetOutput<string>("upper", "result") == "HELLO";
            });

            await Test("ToLower: Converts to lowercase", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("lower", typeof(BaseNodeCollection), "ToLower")
                    .MapInput("input", "\"HELLO\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("lower");
                return result.GetOutput<string>("lower", "result") == "hello";
            });

            await Test("Trim: Removes whitespace", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("trim", typeof(BaseNodeCollection), "Trim")
                    .MapInput("input", "\"  hello  \"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("trim");
                return result.GetOutput<string>("trim", "result") == "hello";
            });

            await Test("Length: Gets string length", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("len", typeof(BaseNodeCollection), "Length")
                    .MapInput("input", "\"hello\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("len");
                return result.GetOutput<int>("len", "result") == 5;
            });

            await Test("Contains: String contains substring", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("contains", typeof(BaseNodeCollection), "Contains")
                    .MapInput("input", "\"hello world\"")
                    .MapInput("value", "\"world\"")
                    .MapInput("ignoreCase", "false")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("contains");
                return result.GetOutput<bool>("contains", "result") == true;
            });

            await Test("StartsWith: String starts with prefix", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("starts", typeof(BaseNodeCollection), "StartsWith")
                    .MapInput("input", "\"hello world\"")
                    .MapInput("value", "\"hello\"")
                    .MapInput("ignoreCase", "false")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("starts");
                return result.GetOutput<bool>("starts", "result") == true;
            });

            await Test("EndsWith: String ends with suffix", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("ends", typeof(BaseNodeCollection), "EndsWith")
                    .MapInput("input", "\"hello world\"")
                    .MapInput("value", "\"world\"")
                    .MapInput("ignoreCase", "false")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("ends");
                return result.GetOutput<bool>("ends", "result") == true;
            });

            await Test("Substring: Extracts substring", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("sub", typeof(BaseNodeCollection), "Substring")
                    .MapInput("input", "\"hello world\"")
                    .MapInput("startIndex", "6")
                    .MapInput("length", "5")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("sub");
                return result.GetOutput<string>("sub", "result") == "world";
            });

            await Test("Replace: Replaces substring", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("replace", typeof(BaseNodeCollection), "Replace")
                    .MapInput("input", "\"hello world\"")
                    .MapInput("oldValue", "\"world\"")
                    .MapInput("newValue", "\"there\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("replace");
                return result.GetOutput<string>("replace", "result") == "hello there";
            });

            await Test("IndexOf: Finds index of substring", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("indexOf", typeof(BaseNodeCollection), "IndexOf")
                    .MapInput("input", "\"hello world\"")
                    .MapInput("value", "\"world\"")
                    .MapInput("ignoreCase", "false")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("indexOf");
                return result.GetOutput<int>("indexOf", "result") == 6;
            });
        }

        private static async Task TestLogicNodes()
        {
            Console.WriteLine("\n[Logic Nodes]");

            await Test("And: True && True", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("and", typeof(BaseNodeCollection), "And")
                    .MapInput("a", "true")
                    .MapInput("b", "true")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("and");
                return result.GetOutput<bool>("and", "result") == true;
            });

            await Test("And: True && False", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("and", typeof(BaseNodeCollection), "And")
                    .MapInput("a", "true")
                    .MapInput("b", "false")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("and");
                return result.GetOutput<bool>("and", "result") == false;
            });

            await Test("Or: False || True", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("or", typeof(BaseNodeCollection), "Or")
                    .MapInput("a", "false")
                    .MapInput("b", "true")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("or");
                return result.GetOutput<bool>("or", "result") == true;
            });

            await Test("Xor: True ^ False", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("xor", typeof(BaseNodeCollection), "Xor")
                    .MapInput("a", "true")
                    .MapInput("b", "false")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("xor");
                return result.GetOutput<bool>("xor", "result") == true;
            });

            await Test("Xor: True ^ True", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("xor", typeof(BaseNodeCollection), "Xor")
                    .MapInput("a", "true")
                    .MapInput("b", "true")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("xor");
                return result.GetOutput<bool>("xor", "result") == false;
            });

            await Test("Not: Inverts true", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("not", typeof(BaseNodeCollection), "Not")
                    .MapInput("value", "true")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("not");
                return result.GetOutput<bool>("not", "result") == false;
            });
        }

        private static async Task TestParsingNodes()
        {
            Console.WriteLine("\n[Parsing Nodes]");

            await Test("ParseInt: Valid integer", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("parse", typeof(BaseNodeCollection), "ParseInt")
                    .MapInput("text", "\"42\"")
                    .MapInput("default", "0")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("parse");
                return result.GetOutput<int>("parse", "result") == 42;
            });

            await Test("ParseInt: Invalid returns default", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("parse", typeof(BaseNodeCollection), "ParseInt")
                    .MapInput("text", "\"invalid\"")
                    .MapInput("default", "99")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("parse");
                return result.GetOutput<int>("parse", "result") == 99;
            });

            await Test("ParseDouble: Valid double", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("parse", typeof(BaseNodeCollection), "ParseDouble")
                    .MapInput("text", "\"3.14\"")
                    .MapInput("default", "0.0")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("parse");
                return result.GetOutput<double>("parse", "result") == 3.14;
            });

            await Test("ParseBool: True", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("parse", typeof(BaseNodeCollection), "ParseBool")
                    .MapInput("text", "\"true\"")
                    .MapInput("default", "false")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("parse");
                return result.GetOutput<bool>("parse", "result") == true;
            });

            await Test("IntToString: Converts int to string", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("conv", typeof(BaseNodeCollection), "IntToString")
                    .MapInput("value", "42")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("conv");
                return result.GetOutput<string>("conv", "result") == "42";
            });

            await Test("IntToDouble: Converts int to double", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("conv", typeof(BaseNodeCollection), "IntToDouble")
                    .MapInput("value", "42")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("conv");
                return result.GetOutput<double>("conv", "result") == 42.0;
            });

            await Test("DoubleToInt: Truncates double", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("conv", typeof(BaseNodeCollection), "DoubleToInt")
                    .MapInput("value", "3.9")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("conv");
                return result.GetOutput<int>("conv", "result") == 3;
            });

            await Test("BoolToInt: True to 1", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("conv", typeof(BaseNodeCollection), "BoolToInt")
                    .MapInput("value", "true")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("conv");
                return result.GetOutput<int>("conv", "result") == 1;
            });

            await Test("BoolToInt: False to 0", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("conv", typeof(BaseNodeCollection), "BoolToInt")
                    .MapInput("value", "false")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("conv");
                return result.GetOutput<int>("conv", "result") == 0;
            });

            await Test("IntToBool: 0 to false", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("conv", typeof(BaseNodeCollection), "IntToBool")
                    .MapInput("value", "0")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("conv");
                return result.GetOutput<bool>("conv", "result") == false;
            });

            await Test("IntToBool: Non-zero to true", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("conv", typeof(BaseNodeCollection), "IntToBool")
                    .MapInput("value", "42")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("conv");
                return result.GetOutput<bool>("conv", "result") == true;
            });
        }

        private static async Task TestDateTimeNodes()
        {
            Console.WriteLine("\n[DateTime Nodes]");

            await Test("UtcNow: Returns current UTC time", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("now", typeof(BaseNodeCollection), "UtcNow")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("now");
                // Just check that we got a DateTime back with curated properties
                var year = result.GetOutput<int>("now", "Year");
                return year >= 2025; // Current year or later
            });

            await Test("AddSeconds: Adds seconds to DateTime", async () =>
            {
                // Note: This test assumes DateTime is treated as curated type or gets proper output mapping
                var graph = new NodeGraphBuilder();
                var baseDate = new DateTime(2025, 1, 1, 0, 0, 0);

                graph.AddNode("add", typeof(BaseNodeCollection), "AddSeconds")
                    .MapInput("dateTime", $"\"{baseDate:O}\"")
                    .MapInput("seconds", "60")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("add");
                // With curated properties, should have Second and Minute properties
                // Adding 60 seconds to 00:00:00 should give 00:01:00 (Minute=1, Second=0)
                var second = result.GetOutput<int>("add", "Second");
                var minute = result.GetOutput<int>("add", "Minute");
                return second == 0 && minute == 1;
            });
        }

        private static async Task TestCollectionNodes()
        {
            Console.WriteLine("\n[Collection Nodes]");

            // Note: Arrays should be treated as single values now, not expose Length property
            await Test("ArrayLength: Gets array length", async () =>
            {
                var graph = new NodeGraphBuilder();
                // Arrays as JSON
                graph.AddNode("len", typeof(BaseNodeCollection), "ArrayLength")
                    .MapInput("items", "[\"a\", \"b\", \"c\"]")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("len");
                return result.GetOutput<int>("len", "result") == 3;
            });
        }

        private static async Task TestJsonNodes()
        {
            Console.WriteLine("\n[JSON Nodes]");

            await Test("JsonSetString: Sets string value", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("set", typeof(BaseNodeCollection), "JsonSetString")
                    .MapInput("obj", "{}")
                    .MapInput("path", "\"name\"")
                    .MapInput("value", "\"John\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("set");
                var output = result.GetOutput<JsonObject>("set", "result");
                return output?["name"]?.GetValue<string>() == "John";
            });
        }

        private static async Task TestControlFlowNodes()
        {
            Console.WriteLine("\n[Control Flow Nodes]");

            await Test("If: True branch", async () =>
            {
                var graph = new NodeGraphBuilder();

                // Add target node first
                graph.AddNode("onTrue", typeof(BaseNodeCollection), "IntVariable")
                    .MapInput("constantInt", "42")
                    .AutoMapOutputs();

                // Then add If node and connect
                graph.AddNode("if", typeof(BaseNodeCollection), "If")
                    .MapInput("condition", "true")
                    .WithOutputPorts("true", "false")
                    .ConnectTo("onTrue", "true");

                var result = await graph.ExecuteAsync("if");
                var value = result.GetOutput<int>("onTrue", "result");
                return value == 42;
            });

            await Test("If: False branch", async () =>
            {
                var graph = new NodeGraphBuilder();

                // Add target node first
                graph.AddNode("onFalse", typeof(BaseNodeCollection), "IntVariable")
                    .MapInput("constantInt", "99")
                    .AutoMapOutputs();

                // Then add If node and connect
                graph.AddNode("if", typeof(BaseNodeCollection), "If")
                    .MapInput("condition", "false")
                    .WithOutputPorts("true", "false")
                    .ConnectTo("onFalse", "false");

                var result = await graph.ExecuteAsync("if");
                var value = result.GetOutput<int>("onFalse", "result");
                return value == 99;
            });

            await Test("For: Loop execution", async () =>
            {
                var graph = new NodeGraphBuilder();

                // Add target node first
                graph.AddNode("done", typeof(BaseNodeCollection), "IntVariable")
                    .MapInput("constantInt", "100")
                    .AutoMapOutputs();

                // Then add For node and connect
                graph.AddNode("for", typeof(BaseNodeCollection), "For")
                    .MapInput("start", "0")
                    .MapInput("end", "3")
                    .WithOutputPorts("loop", "done")
                    .ConnectTo("done", "done");

                var result = await graph.ExecuteAsync("for");
                var value = result.GetOutput<int>("done", "result");
                return value == 100;
            });
        }

        private static async Task TestVariableNodes()
        {
            Console.WriteLine("\n[Variable Nodes]");

            await Test("IntVariable: Returns constant", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("var", typeof(BaseNodeCollection), "IntVariable")
                    .MapInput("constantInt", "42")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("var");
                return result.GetOutput<int>("var", "result") == 42;
            });

            await Test("StringVariable: Returns constant", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("var", typeof(BaseNodeCollection), "StringVariable")
                    .MapInput("constantString", "\"Hello\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("var");
                return result.GetOutput<string>("var", "result") == "Hello";
            });

            await Test("BoolVariable: Returns constant", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("var", typeof(BaseNodeCollection), "BoolVariable")
                    .MapInput("constantBool", "true")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("var");
                return result.GetOutput<bool>("var", "result") == true;
            });
        }

        private static async Task TestRandomNodes()
        {
            Console.WriteLine("\n[Random Nodes]");

            await Test("RandomIntegerRange: Within range", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("rand", typeof(BaseNodeCollection), "RandomIntegerRange")
                    .MapInput("min", "1")
                    .MapInput("max", "10")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("rand");
                var value = result.GetOutput<int>("rand", "result");
                return value >= 1 && value < 10;
            });

            await Test("RandomBool: Returns bool", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("rand", typeof(BaseNodeCollection), "RandomBool")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("rand");
                var value = result.GetOutput<bool>("rand", "result");
                return value == true || value == false; // Just check it returns a bool
            });
        }

        private static async Task TestNewStringUtilities()
        {
            Console.WriteLine("\n[New String Utilities]");

            await Test("Split: Splits string by separator", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("split", typeof(BaseNodeCollection), "Split")
                    .MapInput("input", "\"apple,banana,cherry\"")
                    .MapInput("separator", "\",\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("split");
                var arr = result.GetOutput<string[]>("split", "result");
                return arr.Length == 3 && arr[0] == "apple" && arr[1] == "banana" && arr[2] == "cherry";
            });

            await Test("Reverse: Reverses string", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("rev", typeof(BaseNodeCollection), "Reverse")
                    .MapInput("input", "\"hello\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("rev");
                return result.GetOutput<string>("rev", "result") == "olleh";
            });

            await Test("PadLeft: Pads string on left", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("pad", typeof(BaseNodeCollection), "PadLeft")
                    .MapInput("input", "\"5\"")
                    .MapInput("totalWidth", "3")
                    .MapInput("paddingChar", "\"0\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("pad");
                return result.GetOutput<string>("pad", "result") == "005";
            });

            await Test("PadRight: Pads string on right", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("pad", typeof(BaseNodeCollection), "PadRight")
                    .MapInput("input", "\"5\"")
                    .MapInput("totalWidth", "3")
                    .MapInput("paddingChar", "\"0\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("pad");
                return result.GetOutput<string>("pad", "result") == "500";
            });

            await Test("FormatString: Formats string with args", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("format", typeof(BaseNodeCollection), "FormatString")
                    .MapInput("template", "\"Hello {0}, you are {1} years old!\"")
                    .MapInput("arg0", "\"Alice\"")
                    .MapInput("arg1", "\"25\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("format");
                return result.GetOutput<string>("format", "result") == "Hello Alice, you are 25 years old!";
            });
        }

        private static async Task TestNewArrayUtilities()
        {
            Console.WriteLine("\n[New Array Utilities]");

            await Test("ArrayJoin: Joins array with separator", async () =>
            {
                var graph = new NodeGraphBuilder();

                // Add join node first
                graph.AddNode("join", typeof(BaseNodeCollection), "ArrayJoin")
                    .MapInput("items", "input.result")
                    .MapInput("separator", "\"-\"")
                    .AutoMapOutputs();

                // Then add split and connect
                graph.AddNode("split", typeof(BaseNodeCollection), "Split")
                    .MapInput("input", "\"a,b,c\"")
                    .MapInput("separator", "\",\"")
                    .AutoMapOutputs()
                    .ConnectTo("join");

                var result = await graph.ExecuteAsync("split");
                return result.GetOutput<string>("join", "result") == "a-b-c";
            });

            await Test("ArrayFirst: Gets first element", async () =>
            {
                var graph = new NodeGraphBuilder();

                // Add first node first
                graph.AddNode("first", typeof(BaseNodeCollection), "ArrayFirst")
                    .MapInput("items", "input.result")
                    .AutoMapOutputs();

                // Then add split and connect
                graph.AddNode("split", typeof(BaseNodeCollection), "Split")
                    .MapInput("input", "\"one,two,three\"")
                    .MapInput("separator", "\",\"")
                    .AutoMapOutputs()
                    .ConnectTo("first");

                var result = await graph.ExecuteAsync("split");
                var firstElement = result.GetOutput<JsonNode>("first", "result");
                return firstElement?.GetValue<string>() == "one";
            });

            await Test("ArrayLast: Gets last element", async () =>
            {
                var graph = new NodeGraphBuilder();

                graph.AddNode("last", typeof(BaseNodeCollection), "ArrayLast")
                    .MapInput("items", "input.result")
                    .AutoMapOutputs();

                graph.AddNode("split", typeof(BaseNodeCollection), "Split")
                    .MapInput("input", "\"one,two,three\"")
                    .MapInput("separator", "\",\"")
                    .AutoMapOutputs()
                    .ConnectTo("last");

                var result = await graph.ExecuteAsync("split");
                var lastElement = result.GetOutput<JsonNode>("last", "result");
                return lastElement?.GetValue<string>() == "three";
            });

            await Test("ArrayCount: Gets array length", async () =>
            {
                var graph = new NodeGraphBuilder();

                graph.AddNode("count", typeof(BaseNodeCollection), "ArrayCount")
                    .MapInput("items", "input.result")
                    .AutoMapOutputs();

                graph.AddNode("split", typeof(BaseNodeCollection), "Split")
                    .MapInput("input", "\"a,b,c,d\"")
                    .MapInput("separator", "\",\"")
                    .AutoMapOutputs()
                    .ConnectTo("count");

                var result = await graph.ExecuteAsync("split");
                return result.GetOutput<int>("count", "result") == 4;
            });

            await Test("ArrayReverse: Reverses array", async () =>
            {
                var graph = new NodeGraphBuilder();

                graph.AddNode("reverse", typeof(BaseNodeCollection), "ArrayReverse")
                    .MapInput("items", "input.result")
                    .AutoMapOutputs();

                graph.AddNode("split", typeof(BaseNodeCollection), "Split")
                    .MapInput("input", "\"1,2,3\"")
                    .MapInput("separator", "\",\"")
                    .AutoMapOutputs()
                    .ConnectTo("reverse");

                var result = await graph.ExecuteAsync("split");
                var arr = result.GetOutput<JsonArray>("reverse", "result");
                return arr.Count == 3 && arr[0]?.GetValue<string>() == "3" && arr[1]?.GetValue<string>() == "2" && arr[2]?.GetValue<string>() == "1";
            });

            await Test("ArrayGet: Gets element at index", async () =>
            {
                var graph = new NodeGraphBuilder();

                graph.AddNode("get", typeof(BaseNodeCollection), "ArrayGet")
                    .MapInput("items", "input.result")
                    .MapInput("index", "1")
                    .AutoMapOutputs();

                graph.AddNode("split", typeof(BaseNodeCollection), "Split")
                    .MapInput("input", "\"a,b,c\"")
                    .MapInput("separator", "\",\"")
                    .AutoMapOutputs()
                    .ConnectTo("get");

                var result = await graph.ExecuteAsync("split");
                var element = result.GetOutput<JsonNode>("get", "result");
                return element?.GetValue<string>() == "b";
            });

            await Test("ArraySlice: Gets slice of array", async () =>
            {
                var graph = new NodeGraphBuilder();

                graph.AddNode("slice", typeof(BaseNodeCollection), "ArraySlice")
                    .MapInput("items", "input.result")
                    .MapInput("start", "1")
                    .MapInput("count", "3")
                    .AutoMapOutputs();

                graph.AddNode("split", typeof(BaseNodeCollection), "Split")
                    .MapInput("input", "\"a,b,c,d,e\"")
                    .MapInput("separator", "\",\"")
                    .AutoMapOutputs()
                    .ConnectTo("slice");

                var result = await graph.ExecuteAsync("split");
                var arr = result.GetOutput<JsonArray>("slice", "result");
                return arr.Count == 3 && arr[0]?.GetValue<string>() == "b" && arr[1]?.GetValue<string>() == "c" && arr[2]?.GetValue<string>() == "d";
            });
        }

        private static async Task TestNewLogicUtilities()
        {
            Console.WriteLine("\n[New Logic Utilities]");

            await Test("IsEmpty: Checks empty string", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("check", typeof(BaseNodeCollection), "IsEmpty")
                    .MapInput("input", "\"\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("check");
                return result.GetOutput<bool>("check", "result") == true;
            });

            await Test("IsEmpty: Non-empty string", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("check", typeof(BaseNodeCollection), "IsEmpty")
                    .MapInput("input", "\"hello\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("check");
                return result.GetOutput<bool>("check", "result") == false;
            });

            await Test("IsWhitespace: Checks whitespace string", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("check", typeof(BaseNodeCollection), "IsWhitespace")
                    .MapInput("input", "\"   \"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("check");
                return result.GetOutput<bool>("check", "result") == true;
            });

            await Test("Ternary: Returns true value", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("tern", typeof(BaseNodeCollection), "Ternary")
                    .MapInput("condition", "true")
                    .MapInput("trueValue", "\"yes\"")
                    .MapInput("falseValue", "\"no\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("tern");
                return result.GetOutput<string>("tern", "result") == "yes";
            });

            await Test("Ternary: Returns false value", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("tern", typeof(BaseNodeCollection), "Ternary")
                    .MapInput("condition", "false")
                    .MapInput("trueValue", "\"yes\"")
                    .MapInput("falseValue", "\"no\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("tern");
                return result.GetOutput<string>("tern", "result") == "no";
            });

            await Test("TernaryInt: Returns true value", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("tern", typeof(BaseNodeCollection), "TernaryInt")
                    .MapInput("condition", "true")
                    .MapInput("trueValue", "100")
                    .MapInput("falseValue", "200")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("tern");
                return result.GetOutput<int>("tern", "result") == 100;
            });
        }

        private static async Task TestNewMathUtilities()
        {
            Console.WriteLine("\n[New Math Utilities]");

            await Test("Sum: Sums array of numbers", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("sum", typeof(BaseNodeCollection), "Sum")
                    .MapInput("numbers", "[1, 2, 3, 4, 5]")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("sum");
                return result.GetOutput<double>("sum", "result") == 15.0;
            });

            await Test("Average: Averages array of numbers", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("avg", typeof(BaseNodeCollection), "Average")
                    .MapInput("numbers", "[10, 20, 30]")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("avg");
                return result.GetOutput<double>("avg", "result") == 20.0;
            });

            await Test("AbsDiff: Absolute difference", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("diff", typeof(BaseNodeCollection), "AbsDiff")
                    .MapInput("a", "5")
                    .MapInput("b", "15")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("diff");
                return result.GetOutput<double>("diff", "result") == 10.0;
            });

            await Test("MinOf: Minimum of array", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("min", typeof(BaseNodeCollection), "MinOf")
                    .MapInput("numbers", "[5, 2, 8, 1, 9]")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("min");
                return result.GetOutput<double>("min", "result") == 1.0;
            });

            await Test("MaxOf: Maximum of array", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("max", typeof(BaseNodeCollection), "MaxOf")
                    .MapInput("numbers", "[5, 2, 8, 1, 9]")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("max");
                return result.GetOutput<double>("max", "result") == 9.0;
            });

            await Test("Sum: Works with decimals", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("sum", typeof(BaseNodeCollection), "Sum")
                    .MapInput("numbers", "[1.5, 2.5, 3.0]")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("sum");
                return result.GetOutput<double>("sum", "result") == 7.0;
            });

            await Test("ArrayJoin: Works with numbers", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("join", typeof(BaseNodeCollection), "ArrayJoin")
                    .MapInput("items", "[1, 2, 3, 4, 5]")
                    .MapInput("separator", "\", \"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("join");
                return result.GetOutput<string>("join", "result") == "1, 2, 3, 4, 5";
            });
        }

        private static async Task TestNewDateTimeUtilities()
        {
            Console.WriteLine("\n[New DateTime Utilities]");

            // Note: ParseDateTime already exists in Parsing category, skipping duplicate test

            await Test("DateDiffDays: Difference in days", async () =>
            {
                var graph = new NodeGraphBuilder();
                var date1 = new DateTime(2025, 1, 1);
                var date2 = new DateTime(2025, 1, 11);

                graph.AddNode("diff", typeof(BaseNodeCollection), "DateDiffDays")
                    .MapInput("startDate", $"\"{date1:O}\"")
                    .MapInput("endDate", $"\"{date2:O}\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("diff");
                return result.GetOutput<double>("diff", "result") == 10.0;
            });

            await Test("DateDiffHours: Difference in hours", async () =>
            {
                var graph = new NodeGraphBuilder();
                var date1 = new DateTime(2025, 1, 1, 0, 0, 0);
                var date2 = new DateTime(2025, 1, 1, 5, 0, 0);

                graph.AddNode("diff", typeof(BaseNodeCollection), "DateDiffHours")
                    .MapInput("startDate", $"\"{date1:O}\"")
                    .MapInput("endDate", $"\"{date2:O}\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("diff");
                return result.GetOutput<double>("diff", "result") == 5.0;
            });

            await Test("FormatDateTime: Formats date", async () =>
            {
                var graph = new NodeGraphBuilder();
                var date = new DateTime(2025, 3, 15, 14, 30, 0);

                graph.AddNode("format", typeof(BaseNodeCollection), "FormatDateTime")
                    .MapInput("dateTime", $"\"{date:O}\"")
                    .MapInput("format", "\"yyyy-MM-dd\"")
                    .AutoMapOutputs();

                var result = await graph.ExecuteAsync("format");
                return result.GetOutput<string>("format", "result") == "2025-03-15";
            });
        }

        private static async Task TestFlowSerialization()
        {
            Console.WriteLine("\n[Flow Serialization]");

            await Test("Serialize and deserialize simple flow", async () =>
            {
                // Create a simple single-node flow
                var graph = new NodeGraphBuilder();

                graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                    .MapInput("a", "10")
                    .MapInput("b", "20")
                    .AutoMapOutputs();

                // Execute to verify it works
                var originalResult = await graph.ExecuteAsync("add");
                var originalOutput = originalResult.GetOutput<int>("add", "result");

                if (originalOutput != 30) return false;

                // Serialize the flow
                var nodes = graph.GetAllNodes();
                var json = FlowSerializer.SerializeFlow(nodes, "Test Flow");

                // Verify JSON is not empty
                if (string.IsNullOrWhiteSpace(json)) return false;

                // Deserialize the flow
                var restoredNodes = FlowSerializer.DeserializeFlow(json, out var metadata);

                // Verify metadata
                if (metadata.FlowName != "Test Flow") return false;
                if (restoredNodes.Count != 1) return false;

                // Clear results to test re-execution
                foreach (var node in restoredNodes)
                {
                    node.Result = null;
                }

                // Execute the restored flow
                var restoredNode = restoredNodes.First();
                await restoredNode.ExecuteNode();

                var restoredOutput = restoredNode.Result?.GetByPath("output.result")?.GetValue<int>() ?? -1;

                return restoredOutput == 30;
            });

            await Test("Preserve node mappings", async () =>
            {
                var graph = new NodeGraphBuilder();

                graph.AddNode("concat", typeof(BaseNodeCollection), "StringConcat")
                    .MapInput("input1", "\"Hello\"")
                    .MapInput("input2", "\" World\"")
                    .AutoMapOutputs();

                var nodes = graph.GetAllNodes();
                var node = nodes.First();

                // Verify original mappings
                if (node.NodeInputToMethodInputMap.Count != 2) return false;

                var json = FlowSerializer.SerializeFlow(nodes);
                var restored = FlowSerializer.DeserializeFlow(json);

                var restoredNode = restored.First();

                // Verify mappings are preserved
                if (restoredNode.NodeInputToMethodInputMap.Count != 2) return false;

                var mapping1 = restoredNode.NodeInputToMethodInputMap.First(m => m.To == "input1");
                var mapping2 = restoredNode.NodeInputToMethodInputMap.First(m => m.To == "input2");

                return mapping1.From == "\"Hello\"" && mapping2.From == "\" World\"";
            });

            await Test("Preserve complex connections", async () =>
            {
                var graph = new NodeGraphBuilder();

                // Create a diamond pattern: A -> B -> D
                //                           A -> C -> D
                graph.AddNode("d", typeof(BaseNodeCollection), "Add")
                    .MapInput("a", "input.result")
                    .MapInput("b", "input.result")
                    .AutoMapOutputs();

                graph.AddNode("b", typeof(BaseNodeCollection), "Multiply")
                    .MapInput("a", "input.result")
                    .MapInput("b", "2")
                    .AutoMapOutputs()
                    .ConnectTo("d");

                graph.AddNode("c", typeof(BaseNodeCollection), "Multiply")
                    .MapInput("a", "input.result")
                    .MapInput("b", "3")
                    .AutoMapOutputs()
                    .ConnectTo("d");

                graph.AddNode("a", typeof(BaseNodeCollection), "IntVariable")
                    .MapInput("constantInt", "10")
                    .AutoMapOutputs()
                    .ConnectTo("b")
                    .ConnectTo("c");

                var nodes = graph.GetAllNodes();
                var json = FlowSerializer.SerializeFlow(nodes);
                var restored = FlowSerializer.DeserializeFlow(json);

                // Verify node count
                if (restored.Count != 4) return false;

                // Find nodes
                var aNode = restored.First(n => n.BackingMethod.Name == "IntVariable");
                var bNode = restored.First(n => n.BackingMethod.Name == "Multiply" && n.NodeInputToMethodInputMap.Any(m => m.From == "2"));
                var cNode = restored.First(n => n.BackingMethod.Name == "Multiply" && n.NodeInputToMethodInputMap.Any(m => m.From == "3"));
                var dNode = restored.First(n => n.BackingMethod.Name == "Add");

                // Verify connections
                if (aNode.OutputNodes.Count != 2) return false;
                if (!bNode.InputNodes.Contains(aNode)) return false;
                if (!cNode.InputNodes.Contains(aNode)) return false;
                if (dNode.InputNodes.Count != 2) return false;

                return true;
            });

            await Test("Validate flow JSON", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("test", typeof(BaseNodeCollection), "Add")
                    .MapInput("a", "1")
                    .MapInput("b", "2")
                    .AutoMapOutputs();

                var nodes = graph.GetAllNodes();
                var json = FlowSerializer.SerializeFlow(nodes);

                // Valid JSON should pass validation
                if (!FlowSerializer.ValidateFlow(json, out var error)) return false;

                // Invalid JSON should fail
                if (FlowSerializer.ValidateFlow("{invalid json}", out error)) return false;
                if (string.IsNullOrEmpty(error)) return false;

                // Empty nodes should fail
                if (FlowSerializer.ValidateFlow("{\"Version\":\"1.0\",\"FlowName\":\"Test\",\"CreatedAt\":\"2025-01-01T00:00:00Z\",\"Metadata\":{},\"Nodes\":[]}", out error)) return false;

                return await Task.FromResult(true);
            });

            await Test("Preserve execution state", async () =>
            {
                var graph = new NodeGraphBuilder();
                graph.AddNode("add", typeof(BaseNodeCollection), "Add")
                    .MapInput("a", "100")
                    .MapInput("b", "200")
                    .AutoMapOutputs();

                // Execute the node
                var result = await graph.ExecuteAsync("add");

                var nodes = graph.GetAllNodes();
                var node = nodes.First();

                // Verify execution state is present
                if (node.Result == null) return false;

                // Serialize with execution state
                var json = FlowSerializer.SerializeFlow(nodes);
                var restored = FlowSerializer.DeserializeFlow(json);

                var restoredNode = restored.First();

                // Verify execution state was preserved
                if (restoredNode.Result == null) return false;
                var restoredResult = restoredNode.Result.GetByPath("output.result")?.GetValue<int>() ?? -1;

                return restoredResult == 300;
            });
        }

        private static async Task Test(string name, Func<Task<bool>> test)
        {
            try
            {
                var passed = await test();
                if (passed)
                {
                    PassedTests.Add(name);
                    Console.WriteLine($"  ✓ {name}");
                }
                else
                {
                    FailedTests.Add($"{name}: Assertion failed");
                    Console.WriteLine($"  ✗ {name}: Assertion failed");
                }
            }
            catch (Exception ex)
            {
                FailedTests.Add($"{name}: {ex.Message}");
                Console.WriteLine($"  ✗ {name}: {ex.Message}");
            }
        }

        private static void PrintSummary()
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════╗");
            Console.WriteLine("║                  Test Summary                      ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════╝");
            Console.WriteLine($"\nPassed: {PassedTests.Count}");
            Console.WriteLine($"Failed: {FailedTests.Count}");
            Console.WriteLine($"Total:  {PassedTests.Count + FailedTests.Count}");

            if (FailedTests.Count > 0)
            {
                Console.WriteLine("\n[Failed Tests]");
                foreach (var failure in FailedTests)
                {
                    Console.WriteLine($"  ✗ {failure}");
                }
            }

            Console.WriteLine();
        }
    }
}
