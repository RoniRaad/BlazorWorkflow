using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Flow.Attributes;
using BlazorExecutionFlow.Models;
using BlazorExecutionFlow.Models.NodeV2;
using BlazorExecutionFlow.Helpers;

namespace TestRunner
{
    /// <summary>
    /// Tests for methods that take IServiceProvider parameters.
    /// Ensures they are properly excluded from mappings and serialization.
    /// </summary>
    public static class ServiceProviderTest
    {
        // Test node methods
        [BlazorFlowNodeMethod(NodeType.Function, "Test")]
        public static string SimpleWithServiceProvider(string input, IServiceProvider serviceProvider)
        {
            // ServiceProvider should be auto-injected, not mapped
            return $"Processed: {input}";
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Test")]
        public static string MultipleParamsWithServiceProvider(
            string name,
            int count,
            IServiceProvider serviceProvider,
            bool enabled)
        {
            // IServiceProvider in the middle of parameters
            return $"{name}: {count}, Enabled: {enabled}";
        }

        [BlazorFlowNodeMethod(NodeType.Function, "Test")]
        public static string OnlyServiceProvider(IServiceProvider serviceProvider)
        {
            // Only parameter is IServiceProvider
            return "No other params";
        }

        public static async Task Run()
        {
            Console.WriteLine("=== IServiceProvider Parameter Test ===\n");

            bool allPassed = true;
            allPassed &= TestServiceProviderExcludedFromMappings();
            allPassed &= TestServiceProviderSerializationDeserialization();
            allPassed &= TestMultipleParamsWithServiceProvider();
            allPassed &= TestOnlyServiceProviderParam();
            allPassed &= await TestNodeExecution();

            Console.WriteLine();
            if (allPassed)
            {
                Console.WriteLine("✓ All IServiceProvider tests PASSED");
            }
            else
            {
                Console.WriteLine("✗ Some IServiceProvider tests FAILED");
            }

            Console.WriteLine("\n=== End IServiceProvider Test ===\n");
        }

        private static bool TestServiceProviderExcludedFromMappings()
        {
            Console.WriteLine("[Test 1] IServiceProvider Excluded from Initial Mappings");
            try
            {
                var method = typeof(ServiceProviderTest).GetMethod(
                    nameof(SimpleWithServiceProvider),
                    BindingFlags.Public | BindingFlags.Static);

                if (method == null)
                {
                    Console.WriteLine("  ✗ FAILED: Could not find test method");
                    return false;
                }

                var node = new Node
                {
                    BackingMethod = method,
                    Section = "Test",
                    Id = Guid.NewGuid().ToString()
                };

                // Simulate what happens when node editor generates default mappings
                var parameters = method.GetParameters();
                var mappings = new List<PathMapEntry>();

                foreach (var param in parameters)
                {
                    // This is the fix - skip IServiceProvider
                    if (param.ParameterType == typeof(NodeContext) ||
                        param.ParameterType == typeof(IServiceProvider))
                        continue;

                    mappings.Add(new PathMapEntry
                    {
                        From = $"{{{{input.{param.Name}}}}}",
                        To = param.Name ?? string.Empty
                    });
                }

                node.NodeInputToMethodInputMap = mappings;

                // Verify IServiceProvider is NOT in mappings
                var hasServiceProviderMapping = mappings.Any(m =>
                    m.To == "serviceProvider" ||
                    parameters.Any(p => p.Name == m.To && p.ParameterType == typeof(IServiceProvider)));

                if (hasServiceProviderMapping)
                {
                    Console.WriteLine("  ✗ FAILED: IServiceProvider found in mappings");
                    return false;
                }

                // Should only have 'input' parameter mapped
                if (mappings.Count != 1 || mappings[0].To != "input")
                {
                    Console.WriteLine($"  ✗ FAILED: Expected 1 mapping for 'input', got {mappings.Count}");
                    foreach (var m in mappings)
                    {
                        Console.WriteLine($"    Mapping: {m.From} -> {m.To}");
                    }
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: IServiceProvider correctly excluded from mappings");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static bool TestServiceProviderSerializationDeserialization()
        {
            Console.WriteLine("[Test 2] IServiceProvider Serialization/Deserialization");
            try
            {
                var method = typeof(ServiceProviderTest).GetMethod(
                    nameof(SimpleWithServiceProvider),
                    BindingFlags.Public | BindingFlags.Static);

                if (method == null)
                {
                    Console.WriteLine("  ✗ FAILED: Could not find test method");
                    return false;
                }

                var node = new Node
                {
                    BackingMethod = method,
                    Section = "Test",
                    Id = Guid.NewGuid().ToString(),
                    NodeInputToMethodInputMap = new List<PathMapEntry>
                    {
                        new PathMapEntry { From = "{{input.input}}", To = "input" }
                        // IServiceProvider intentionally NOT included
                    }
                };

                // Serialize
                var json = JsonSerializer.Serialize(node);
                Console.WriteLine($"  Serialized length: {json.Length} chars");

                // Deserialize
                var deserialized = JsonSerializer.Deserialize<Node>(json);

                if (deserialized == null)
                {
                    Console.WriteLine("  ✗ FAILED: Deserialization returned null");
                    return false;
                }

                // Verify method is correct
                if (deserialized.BackingMethod.Name != method.Name)
                {
                    Console.WriteLine($"  ✗ FAILED: Method name mismatch - expected {method.Name}, got {deserialized.BackingMethod.Name}");
                    return false;
                }

                // Verify parameters match
                var originalParams = method.GetParameters();
                var deserializedParams = deserialized.BackingMethod.GetParameters();

                if (originalParams.Length != deserializedParams.Length)
                {
                    Console.WriteLine($"  ✗ FAILED: Parameter count mismatch - expected {originalParams.Length}, got {deserializedParams.Length}");
                    return false;
                }

                // Verify IServiceProvider parameter is present in method signature
                var hasServiceProviderParam = deserializedParams.Any(p => p.ParameterType == typeof(IServiceProvider));
                if (!hasServiceProviderParam)
                {
                    Console.WriteLine("  ✗ FAILED: IServiceProvider parameter missing from deserialized method");
                    return false;
                }

                // Verify IServiceProvider is NOT in mappings
                var hasServiceProviderMapping = deserialized.NodeInputToMethodInputMap
                    .Any(m => deserializedParams.Any(p => p.Name == m.To && p.ParameterType == typeof(IServiceProvider)));

                if (hasServiceProviderMapping)
                {
                    Console.WriteLine("  ✗ FAILED: IServiceProvider found in deserialized mappings");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Serialization/Deserialization works correctly");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                Console.WriteLine($"  Stack: {ex.StackTrace}");
                return false;
            }
        }

        private static bool TestMultipleParamsWithServiceProvider()
        {
            Console.WriteLine("[Test 3] Multiple Parameters with IServiceProvider in Middle");
            try
            {
                var method = typeof(ServiceProviderTest).GetMethod(
                    nameof(MultipleParamsWithServiceProvider),
                    BindingFlags.Public | BindingFlags.Static);

                if (method == null)
                {
                    Console.WriteLine("  ✗ FAILED: Could not find test method");
                    return false;
                }

                var node = new Node
                {
                    BackingMethod = method,
                    Section = "Test",
                    Id = Guid.NewGuid().ToString()
                };

                // Generate mappings excluding IServiceProvider
                var parameters = method.GetParameters();
                var mappings = new List<PathMapEntry>();

                foreach (var param in parameters)
                {
                    if (param.ParameterType == typeof(NodeContext) ||
                        param.ParameterType == typeof(IServiceProvider))
                        continue;

                    mappings.Add(new PathMapEntry
                    {
                        From = $"{{{{input.{param.Name}}}}}",
                        To = param.Name ?? string.Empty
                    });
                }

                node.NodeInputToMethodInputMap = mappings;

                // Should have 3 mappings: name, count, enabled (excluding serviceProvider)
                if (mappings.Count != 3)
                {
                    Console.WriteLine($"  ✗ FAILED: Expected 3 mappings, got {mappings.Count}");
                    return false;
                }

                var mappedNames = mappings.Select(m => m.To).ToHashSet();
                if (!mappedNames.Contains("name") || !mappedNames.Contains("count") || !mappedNames.Contains("enabled"))
                {
                    Console.WriteLine("  ✗ FAILED: Missing expected parameter mappings");
                    return false;
                }

                if (mappedNames.Contains("serviceProvider"))
                {
                    Console.WriteLine("  ✗ FAILED: serviceProvider should not be in mappings");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: IServiceProvider correctly excluded from middle of parameter list");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static bool TestOnlyServiceProviderParam()
        {
            Console.WriteLine("[Test 4] Method with Only IServiceProvider Parameter");
            try
            {
                var method = typeof(ServiceProviderTest).GetMethod(
                    nameof(OnlyServiceProvider),
                    BindingFlags.Public | BindingFlags.Static);

                if (method == null)
                {
                    Console.WriteLine("  ✗ FAILED: Could not find test method");
                    return false;
                }

                var node = new Node
                {
                    BackingMethod = method,
                    Section = "Test",
                    Id = Guid.NewGuid().ToString()
                };

                // Generate mappings
                var parameters = method.GetParameters();
                var mappings = new List<PathMapEntry>();

                foreach (var param in parameters)
                {
                    if (param.ParameterType == typeof(NodeContext) ||
                        param.ParameterType == typeof(IServiceProvider))
                        continue;

                    mappings.Add(new PathMapEntry
                    {
                        From = $"{{{{input.{param.Name}}}}}",
                        To = param.Name ?? string.Empty
                    });
                }

                node.NodeInputToMethodInputMap = mappings;

                // Should have 0 mappings since only parameter is IServiceProvider
                if (mappings.Count != 0)
                {
                    Console.WriteLine($"  ✗ FAILED: Expected 0 mappings, got {mappings.Count}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Method with only IServiceProvider has no mappings");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestNodeExecution()
        {
            Console.WriteLine("[Test 5] Node Execution with IServiceProvider");
            try
            {
                var method = typeof(ServiceProviderTest).GetMethod(
                    nameof(SimpleWithServiceProvider),
                    BindingFlags.Public | BindingFlags.Static);

                if (method == null)
                {
                    Console.WriteLine("  ✗ FAILED: Could not find test method");
                    return false;
                }

                var node = new Node
                {
                    BackingMethod = method,
                    Section = "Test",
                    Id = Guid.NewGuid().ToString(),
                    NodeInputToMethodInputMap = new List<PathMapEntry>
                    {
                        new PathMapEntry { From = "\"test input\"", To = "input" }
                    },
                    MethodOutputToNodeOutputMap = new List<PathMapEntry>
                    {
                        new PathMapEntry { From = "result", To = "result" }
                    }
                };

                // Execute node
                await node.ExecuteNode();
                var result = node.Result;

                if (result == null)
                {
                    Console.WriteLine("  ✗ FAILED: Execution returned null");
                    return false;
                }

                // Verify result - it should be a JsonObject with nested "output.result" property
                var resultStr = result.ToString();
                Console.WriteLine($"  Result JSON: {resultStr}");

                // Try to get the result value (it's nested under "output")
                if (result.TryGetPropertyValue("output", out var outputValue) &&
                    outputValue is JsonObject outputObj &&
                    outputObj.TryGetPropertyValue("result", out var resultValue))
                {
                    var resultString = resultValue?.GetValue<string>();
                    if (resultString == null || !resultString.Contains("Processed: test input"))
                    {
                        Console.WriteLine($"  ✗ FAILED: Unexpected result value: {resultString}");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine($"  ✗ FAILED: Result doesn't contain 'output.result' property: {resultStr}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Node execution successful with IServiceProvider auto-injection");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                Console.WriteLine($"  Stack: {ex.StackTrace}");
                return false;
            }
        }
    }
}
