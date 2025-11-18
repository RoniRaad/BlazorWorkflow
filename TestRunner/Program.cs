using TestRunner;

Console.WriteLine("Starting Test Suite...\n");
Console.WriteLine("=" + new string('=', 78));
Console.WriteLine();

try
{
    // Run comprehensive JSON type handling tests
    JsonTypeHandlingTest.Run();

    Console.WriteLine(new string('-', 80));
    Console.WriteLine();

    // Run comprehensive workflow serialization tests
    WorkflowSerializationTest.Run();

    Console.WriteLine(new string('-', 80));
    Console.WriteLine();

    // Run common workflow integration tests
    await CommonWorkflowTests.Run();

    Console.WriteLine(new string('-', 80));
    Console.WriteLine();

    // Run advanced workflow integration tests
    await AdvancedWorkflowTests.Run();

    Console.WriteLine(new string('-', 80));
    Console.WriteLine();

    // Run iteration output test
    IterationOutputTest.Run();

    Console.WriteLine(new string('-', 80));
    Console.WriteLine();

    // Run serialization test
    SerializationTest.Run();

    Console.WriteLine(new string('-', 80));
    Console.WriteLine();

    // Run boolean handling test
    await BooleanTest.Run();

    Console.WriteLine(new string('-', 80));
    Console.WriteLine();

    // Run performance tests
    await NodeTests.RunAllTests();
}
catch (Exception ex)
{
    Console.WriteLine($"\n\nFatal Error: {ex.Message}");
    Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
    return 1;
}

Console.WriteLine();
Console.WriteLine("=" + new string('=', 78));
Console.WriteLine("All tests completed!");
return 0;
