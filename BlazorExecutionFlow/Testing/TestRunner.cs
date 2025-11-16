namespace BlazorExecutionFlow.Testing
{
    /// <summary>
    /// Simple test runner for executing node tests from console.
    /// Usage: await TestRunner.RunTests();
    /// </summary>
    public static class TestRunner
    {
        public static async Task RunTests()
        {
            Console.WriteLine("Starting Node Test Suite...\n");
            await NodeTests.RunAllTests();
            Console.WriteLine("\nTests completed!");
        }
    }
}
