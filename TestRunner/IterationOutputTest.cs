using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Drawflow.BaseNodes;
using BlazorExecutionFlow.Helpers;

namespace TestRunner
{
    public static class IterationOutputTest
    {
        public static void Run()
        {
            Console.WriteLine("=== Iteration Output Test ===\n");

            // Simulate what ForEachString does
            Console.WriteLine("[Test 1] Simulating ForEachString output");

            var outputData = new JsonObject();
            var iterationOutput = new JsonObject { ["output"] = outputData };

            var item = "test string";
            int i = 5;
            int totalCount = 10;

            // This is what ForEachString currently does
            outputData["currentItem"] = JsonSerializer.SerializeToNode(item);
            outputData["currentIndex"] = i;  // Direct assignment
            outputData["totalCount"] = totalCount;  // Direct assignment
            outputData["isFirst"] = i == 0;  // Direct assignment
            outputData["isLast"] = i == totalCount - 1;  // Direct assignment

            Console.WriteLine($"  Output JSON: {iterationOutput.ToJsonString(new JsonSerializerOptions { WriteIndented = true })}");
            Console.WriteLine();

            // Now check what types we get back
            Console.WriteLine("[Test 2] Checking property types");
            var currentItem = outputData["currentItem"];
            var currentIndex = outputData["currentIndex"];
            var isFirst = outputData["isFirst"];

            Console.WriteLine($"  currentItem type: {currentItem?.GetType().Name}");
            Console.WriteLine($"  currentItem value: {currentItem}");
            Console.WriteLine($"  currentIndex type: {currentIndex?.GetType().Name}");
            Console.WriteLine($"  currentIndex value: {currentIndex}");
            Console.WriteLine($"  isFirst type: {isFirst?.GetType().Name}");
            Console.WriteLine($"  isFirst value: {isFirst}");
            Console.WriteLine();

            // Test ToPlainObject (used by Scriban templates)
            Console.WriteLine("[Test 3] ToPlainObject conversion");
            var plainObject = iterationOutput.ToPlainObject();
            Console.WriteLine($"  Plain object: {JsonSerializer.Serialize(plainObject, new JsonSerializerOptions { WriteIndented = true })}");

            if (plainObject is Dictionary<string, object?> dict &&
                dict.TryGetValue("output", out var outputObj) &&
                outputObj is Dictionary<string, object?> outputDict)
            {
                Console.WriteLine("\n  Output property types:");
                foreach (var kvp in outputDict)
                {
                    Console.WriteLine($"    {kvp.Key}: {kvp.Value?.GetType().Name} = {kvp.Value}");
                }
            }
            Console.WriteLine();

            // Test template access (simulate what Log node does)
            Console.WriteLine("[Test 4] Template access simulation");
            try
            {
                var getByPath1 = iterationOutput.GetByPath("output.currentItem");
                Console.WriteLine($"  GetByPath('output.currentItem'): {getByPath1} (type: {getByPath1?.GetType().Name})");

                var getByPath2 = iterationOutput.GetByPath("output.currentIndex");
                Console.WriteLine($"  GetByPath('output.currentIndex'): {getByPath2} (type: {getByPath2?.GetType().Name})");

                var getByPath3 = iterationOutput.GetByPath("output.isFirst");
                Console.WriteLine($"  GetByPath('output.isFirst'): {getByPath3} (type: {getByPath3?.GetType().Name})");

                // Try to coerce to different types
                Console.WriteLine("\n  Coercion tests:");
                var coercedString = getByPath1?.CoerceToType(typeof(string));
                Console.WriteLine($"    currentItem as string: {coercedString}");

                var coercedInt = getByPath2?.CoerceToType(typeof(int));
                Console.WriteLine($"    currentIndex as int: {coercedInt}");

                var coercedBool = getByPath3?.CoerceToType(typeof(bool));
                Console.WriteLine($"    isFirst as bool: {coercedBool}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: {ex.Message}");
                Console.WriteLine($"  Stack: {ex.StackTrace}");
            }

            Console.WriteLine();
            Console.WriteLine("=== End Iteration Output Test ===\n");
        }
    }
}
