using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Testing;
using Xunit;

namespace TestRunner
{
    /// <summary>
    /// Comprehensive tests for mathematical edge cases and boundary conditions.
    /// </summary>
    public class MathEdgeCaseTests
    {

        // ==========================================
        // SQUARE ROOT EDGE CASES
        // ==========================================

        [Fact]
        public async Task TestSquareRootOfZero()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("sqrt", typeof(BaseNodeCollection), "Sqrt")
                .MapInput("value", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("sqrt");
            Assert.Equal(0.0, result.GetOutput<double>("sqrt", "result"));
        }

        [Fact]
        public async Task TestSquareRootOfVerySmallNumber()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("sqrt", typeof(BaseNodeCollection), "Sqrt")
                .MapInput("value", "0.0000001")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("sqrt");
            Assert.Equal(0.0003162, result.GetOutput<double>("sqrt", "result"), 6);
        }

        // ==========================================
        // POWER EDGE CASES
        // ==========================================

        [Fact]
        public async Task TestZeroToZeroPower()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("power", typeof(BaseNodeCollection), "Pow")
                .MapInput("base", "0")
                .MapInput("exponent", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("power");
            // 0^0 is mathematically undefined but typically returns 1 in most implementations
            var powResult = result.GetOutput<double>("power", "result");
            Assert.True(powResult == 1.0 || double.IsNaN(powResult));
        }

        [Fact]
        public async Task TestNegativeExponent()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("power", typeof(BaseNodeCollection), "Pow")
                .MapInput("base", "2")
                .MapInput("exponent", "-3")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("power");
            Assert.Equal(0.125, result.GetOutput<double>("power", "result"), 6);
        }

        [Fact]
        public async Task TestNegativeBaseWithFractionalExponent()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("power", typeof(BaseNodeCollection), "Pow")
                .MapInput("base", "-8")
                .MapInput("exponent", "0.333333333")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("power");
            var powResult = result.GetOutput<double>("power", "result");
            // Negative base with fractional exponent typically results in NaN
            Assert.True(double.IsNaN(powResult));
        }

        [Fact]
        public async Task TestPowerWithFractionalExponent()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("power", typeof(BaseNodeCollection), "Pow")
                .MapInput("base", "8")
                .MapInput("exponent", "0.333333333")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("power");
            // 8^(1/3) should be close to 2
            Assert.Equal(2.0, result.GetOutput<double>("power", "result"), 1);
        }




        // ==========================================
        // MODULO EDGE CASES
        // ==========================================

        [Fact]
        public async Task TestModuloWithNegativeDividend()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("mod", typeof(BaseNodeCollection), "Modulo")
                .MapInput("input1", "-10")
                .MapInput("input2", "3")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("mod");
            // C# modulo: -10 % 3 = -1
            Assert.Equal(-1, result.GetOutput<int>("mod", "result"));
        }

        [Fact]
        public async Task TestModuloWithNegativeDivisor()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("mod", typeof(BaseNodeCollection), "Modulo")
                .MapInput("input1", "10")
                .MapInput("input2", "-3")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("mod");
            // C# modulo: 10 % -3 = 1
            Assert.Equal(1, result.GetOutput<int>("mod", "result"));
        }

        [Fact]
        public async Task TestModuloWithBothNegative()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("mod", typeof(BaseNodeCollection), "Modulo")
                .MapInput("input1", "-10")
                .MapInput("input2", "-3")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("mod");
            // C# modulo: -10 % -3 = -1
            Assert.Equal(-1, result.GetOutput<int>("mod", "result"));
        }

        [Fact]
        public async Task TestModuloByOne()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("mod", typeof(BaseNodeCollection), "Modulo")
                .MapInput("input1", "42")
                .MapInput("input2", "1")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("mod");
            Assert.Equal(0, result.GetOutput<int>("mod", "result"));
        }

        // ==========================================
        // ABSOLUTE VALUE EDGE CASES
        // ==========================================

        [Fact]
        public async Task TestAbsoluteValueOfNegativeZero()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("abs", typeof(BaseNodeCollection), "Abs")
                .MapInput("value", "-0.0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("abs");
            Assert.Equal(0.0, result.GetOutput<double>("abs", "result"));
        }

        // ==========================================
        // SIGN FUNCTION EDGE CASES
        // ==========================================

        [Fact]
        public async Task TestSignOfZero()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("sign", typeof(BaseNodeCollection), "Sign")
                .MapInput("value", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("sign");
            Assert.Equal(0, result.GetOutput<int>("sign", "result"));
        }

        [Fact]
        public async Task TestSignOfNegative()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("sign", typeof(BaseNodeCollection), "Sign")
                .MapInput("input", "-42.7")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("sign");
            Assert.Equal(-1, result.GetOutput<int>("sign", "result"));
        }

        [Fact]
        public async Task TestSignOfPositive()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("sign", typeof(BaseNodeCollection), "Sign")
                .MapInput("input", "0.001")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("sign");
            Assert.Equal(1, result.GetOutput<int>("sign", "result"));
        }

        [Fact]
        public async Task TestSignOfNegativeZero()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("sign", typeof(BaseNodeCollection), "Sign")
                .MapInput("value", "-0.0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("sign");
            Assert.Equal(0, result.GetOutput<int>("sign", "result"));
        }

        // ==========================================
        // ROUNDING EDGE CASES
        // ==========================================

        [Fact]
        public async Task TestRoundHalfUp()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("round", typeof(BaseNodeCollection), "RoundD")
                .MapInput("value", "2.5")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("round");
            // C# rounds to nearest even (banker's rounding): 2.5 -> 2
            Assert.Equal(2.0, result.GetOutput<double>("round", "result"));
        }

        [Fact]
        public async Task TestRoundHalfDown()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("round", typeof(BaseNodeCollection), "RoundD")
                .MapInput("value", "3.5")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("round");
            // Banker's rounding: 3.5 -> 4
            Assert.Equal(4.0, result.GetOutput<double>("round", "result"));
        }

        [Fact]
        public async Task TestFloorOfNegative()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("floor", typeof(BaseNodeCollection), "FloorD")
                .MapInput("value", "-2.3")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("floor");
            Assert.Equal(-3.0, result.GetOutput<double>("floor", "result"));
        }

        [Fact]
        public async Task TestCeilOfNegative()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("ceil", typeof(BaseNodeCollection), "CeilingD")
                .MapInput("value", "-2.7")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("ceil");
            Assert.Equal(-2.0, result.GetOutput<double>("ceil", "result"));
        }

        [Fact]
        public async Task TestFloorOfExactInteger()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("floor", typeof(BaseNodeCollection), "FloorD")
                .MapInput("value", "5.0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("floor");
            Assert.Equal(5.0, result.GetOutput<double>("floor", "result"));
        }

        [Fact]
        public async Task TestCeilOfExactInteger()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("ceil", typeof(BaseNodeCollection), "CeilingD")
                .MapInput("value", "7.0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("ceil");
            Assert.Equal(7.0, result.GetOutput<double>("ceil", "result"));
        }

        // ==========================================
        // MIN/MAX EDGE CASES
        // ==========================================

        [Fact]
        public async Task TestMinWithEqualValues()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("min", typeof(BaseNodeCollection), "Min")
                .MapInput("a", "5.5")
                .MapInput("b", "5.5")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("min");
            Assert.Equal(5.5, result.GetOutput<double>("min", "result"));
        }

        [Fact]
        public async Task TestMaxWithNegativeNumbers()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("max", typeof(BaseNodeCollection), "Max")
                .MapInput("input1", "-10")
                .MapInput("input2", "-5")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("max");
            Assert.Equal(-5.0, result.GetOutput<double>("max", "result"));
        }

        [Fact]
        public async Task TestMinWithZeroAndNegativeZero()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("min", typeof(BaseNodeCollection), "Min")
                .MapInput("input1", "0.0")
                .MapInput("input2", "-0.0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("min");
            Assert.Equal(0.0, Math.Abs(result.GetOutput<double>("min", "result")));
        }

        // ==========================================
        // LERP EDGE CASES
        // ==========================================

        [Fact]
        public async Task TestLerpAtZero()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("lerp", typeof(BaseNodeCollection), "Lerp")
                .MapInput("start", "10")
                .MapInput("end", "20")
                .MapInput("t", "0")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("lerp");
            Assert.Equal(10.0, result.GetOutput<double>("lerp", "result"));
        }

        [Fact]
        public async Task TestLerpAtOne()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("lerp", typeof(BaseNodeCollection), "Lerp")
                .MapInput("start", "10")
                .MapInput("end", "20")
                .MapInput("t", "1")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("lerp");
            Assert.Equal(20.0, result.GetOutput<double>("lerp", "result"));
        }

        [Fact]
        public async Task TestLerpAtHalf()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("lerp", typeof(BaseNodeCollection), "Lerp")
                .MapInput("start", "10")
                .MapInput("end", "20")
                .MapInput("t", "0.5")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("lerp");
            Assert.Equal(15.0, result.GetOutput<double>("lerp", "result"));
        }

        [Fact]
        public async Task TestLerpExtrapolation()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("lerp", typeof(BaseNodeCollection), "Lerp")
                .MapInput("start", "10")
                .MapInput("end", "20")
                .MapInput("t", "2")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("lerp");
            Assert.Equal(30.0, result.GetOutput<double>("lerp", "result"));
        }

        [Fact]
        public async Task TestLerpWithNegativeT()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("lerp", typeof(BaseNodeCollection), "Lerp")
                .MapInput("start", "10")
                .MapInput("end", "20")
                .MapInput("t", "-0.5")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("lerp");
            Assert.Equal(5.0, result.GetOutput<double>("lerp", "result"));
        }

        [Fact]
        public async Task TestLerpWithSameStartAndEnd()
        {
            var graph = new NodeGraphBuilder();
            graph.AddNode("lerp", typeof(BaseNodeCollection), "Lerp")
                .MapInput("start", "15")
                .MapInput("end", "15")
                .MapInput("t", "0.7")
                .AutoMapOutputs();

            var result = await graph.ExecuteAsync("lerp");
            Assert.Equal(15.0, result.GetOutput<double>("lerp", "result"));
        }
    }
}
