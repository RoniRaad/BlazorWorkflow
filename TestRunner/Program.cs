using DrawflowWrapper.Testing;

Console.WriteLine("Starting Node Test Suite...\n");

try
{
    await NodeTests.RunAllTests();
}
catch (Exception ex)
{
    Console.WriteLine($"\n\nFatal Error: {ex.Message}");
    Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
    return 1;
}

return 0;
