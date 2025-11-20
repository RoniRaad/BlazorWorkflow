using System.Reflection;
using System.Text.Json;
using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models.NodeV2;

namespace TestRunner
{
    /// <summary>
    /// Comprehensive tests for workflow serialization/deserialization to catch
    /// assembly qualified name parsing issues, version mismatches, and edge cases.
    /// </summary>
    public static class WorkflowSerializationTest
    {
        public static void Run()
        {
            Console.WriteLine("=== Workflow Serialization Test ===\n");

            bool allPassed = true;
            allPassed &= TestSimpleMethodSerialization();
            allPassed &= TestGenericMethodSerialization();
            allPassed &= TestMultipleParametersSerialization();
            allPassed &= TestNestedGenericsSerialization();
            allPassed &= TestVersionMismatchHandling();
            allPassed &= TestAssemblyQualifiedNameParsing();

            Console.WriteLine();
            if (allPassed)
            {
                Console.WriteLine("✓ All workflow serialization tests PASSED");
            }
            else
            {
                Console.WriteLine("✗ Some workflow serialization tests FAILED");
            }

            Console.WriteLine("\n=== End Workflow Serialization Test ===\n");
        }

        private static bool TestSimpleMethodSerialization()
        {
            Console.WriteLine("[Test 1] Simple Method Serialization");
            try
            {
                var method = typeof(BaseNodeCollection).GetMethod("Add", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Console.WriteLine("  ✗ FAILED: Could not find Add method");
                    return false;
                }

                var serialized = MethodInfoHelpers.ToSerializableString(method);
                var deserialized = MethodInfoHelpers.FromSerializableString(serialized);

                if (deserialized.Name != method.Name)
                {
                    Console.WriteLine($"  ✗ FAILED: Method name mismatch - expected {method.Name}, got {deserialized.Name}");
                    return false;
                }

                if (deserialized.DeclaringType != method.DeclaringType)
                {
                    Console.WriteLine("  ✗ FAILED: Declaring type mismatch");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Simple method serialization works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static bool TestGenericMethodSerialization()
        {
            Console.WriteLine("[Test 2] Generic Method Serialization");
            try
            {
                // Test with List<string> parameter
                var method = typeof(AdvancedIterationNodes).GetMethod("ForEachString", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Console.WriteLine("  ✗ FAILED: Could not find ForEachString method");
                    return false;
                }

                var serialized = MethodInfoHelpers.ToSerializableString(method);
                Console.WriteLine($"  Serialized: {serialized.Substring(0, Math.Min(100, serialized.Length))}...");

                var deserialized = MethodInfoHelpers.FromSerializableString(serialized);

                if (deserialized.Name != "ForEachString")
                {
                    Console.WriteLine($"  ✗ FAILED: Method name mismatch - got {deserialized.Name}");
                    return false;
                }

                var parameters = deserialized.GetParameters();
                if (parameters.Length != 2)
                {
                    Console.WriteLine($"  ✗ FAILED: Expected 2 parameters, got {parameters.Length}");
                    return false;
                }

                if (!parameters[0].ParameterType.IsGenericType)
                {
                    Console.WriteLine("  ✗ FAILED: First parameter should be generic List");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Generic method serialization works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static bool TestMultipleParametersSerialization()
        {
            Console.WriteLine("[Test 3] Multiple Parameters with Complex Types");
            try
            {
                var method = typeof(AdvancedIterationNodes).GetMethod("ForEachJson", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Console.WriteLine("  ✗ FAILED: Could not find ForEachJson method");
                    return false;
                }

                var serialized = MethodInfoHelpers.ToSerializableString(method);
                var deserialized = MethodInfoHelpers.FromSerializableString(serialized);

                var originalParams = method.GetParameters();
                var deserializedParams = deserialized.GetParameters();

                if (originalParams.Length != deserializedParams.Length)
                {
                    Console.WriteLine($"  ✗ FAILED: Parameter count mismatch - expected {originalParams.Length}, got {deserializedParams.Length}");
                    return false;
                }

                for (int i = 0; i < originalParams.Length; i++)
                {
                    if (originalParams[i].ParameterType.FullName != deserializedParams[i].ParameterType.FullName)
                    {
                        Console.WriteLine($"  ✗ FAILED: Parameter {i} type mismatch");
                        Console.WriteLine($"    Expected: {originalParams[i].ParameterType.FullName}");
                        Console.WriteLine($"    Got: {deserializedParams[i].ParameterType.FullName}");
                        return false;
                    }
                }

                Console.WriteLine("  ✓ PASSED: Multiple parameter serialization works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static bool TestNestedGenericsSerialization()
        {
            Console.WriteLine("[Test 4] Nested Generics Serialization");
            try
            {
                // ForEachString uses List<string> which has nested assembly qualified names
                var method = typeof(AdvancedIterationNodes).GetMethod("ForEachString", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Console.WriteLine("  ✗ FAILED: Could not find ForEachString method");
                    return false;
                }

                var serialized = MethodInfoHelpers.ToSerializableString(method);

                // Check that the serialized string contains brackets (indicating nested types)
                if (!serialized.Contains("[["))
                {
                    Console.WriteLine("  ✗ FAILED: Serialized string doesn't contain nested type markers");
                    return false;
                }

                // Check that it contains multiple commas (potential parsing issue)
                var commaCount = serialized.Count(c => c == ',');
                if (commaCount < 5)
                {
                    Console.WriteLine("  ✗ FAILED: Expected multiple commas in assembly qualified names");
                    return false;
                }

                var deserialized = MethodInfoHelpers.FromSerializableString(serialized);

                if (deserialized.Name != "ForEachString")
                {
                    Console.WriteLine($"  ✗ FAILED: Method name mismatch after nested generics deserialization");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Nested generics serialization works (bracket-aware parsing)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static bool TestVersionMismatchHandling()
        {
            Console.WriteLine("[Test 5] Version Mismatch Handling");
            try
            {
                // Create a serialized string with a different version number
                var method = typeof(AdvancedIterationNodes).GetMethod("ForEachString", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Console.WriteLine("  ✗ FAILED: Could not find ForEachString method");
                    return false;
                }

                var serialized = MethodInfoHelpers.ToSerializableString(method);

                // Replace the current version with a fake old version
                var fakeOldVersion = serialized.Replace("Version=1.0.1.0", "Version=0.9.0.0");
                fakeOldVersion = fakeOldVersion.Replace("Version=10.0.0.0", "Version=8.0.0.0");

                Console.WriteLine("  Testing with modified version numbers...");

                var deserialized = MethodInfoHelpers.FromSerializableString(fakeOldVersion);

                if (deserialized.Name != "ForEachString")
                {
                    Console.WriteLine($"  ✗ FAILED: Method name mismatch with version change");
                    return false;
                }

                var parameters = deserialized.GetParameters();
                if (parameters.Length != 2)
                {
                    Console.WriteLine($"  ✗ FAILED: Parameter count changed with version mismatch");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Version mismatch handled gracefully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static bool TestAssemblyQualifiedNameParsing()
        {
            Console.WriteLine("[Test 6] Assembly Qualified Name Parsing Edge Cases");
            try
            {
                // Test end-to-end with actual method signatures that have tricky assembly qualified names
                var testCases = new[]
                {
                    // Test with ForEachString - has List<string> with nested brackets
                    (typeof(AdvancedIterationNodes).GetMethod("ForEachString", BindingFlags.Public | BindingFlags.Static), 2),

                    // Test with ForEachNumber - has List<double>
                    (typeof(AdvancedIterationNodes).GetMethod("ForEachNumber", BindingFlags.Public | BindingFlags.Static), 2),

                    // Test with ForEachJson - has List<JsonNode>
                    (typeof(AdvancedIterationNodes).GetMethod("ForEachJson", BindingFlags.Public | BindingFlags.Static), 2),

                    // Test with MapStrings
                    (typeof(AdvancedIterationNodes).GetMethod("MapStrings", BindingFlags.Public | BindingFlags.Static), 2),
                };

                foreach (var (method, expectedParamCount) in testCases)
                {
                    if (method == null)
                    {
                        Console.WriteLine("  ✗ FAILED: Could not find test method");
                        return false;
                    }

                    // Serialize and deserialize
                    var serialized = MethodInfoHelpers.ToSerializableString(method);
                    var deserialized = MethodInfoHelpers.FromSerializableString(serialized);

                    // Verify parameter count matches
                    if (deserialized.GetParameters().Length != expectedParamCount)
                    {
                        Console.WriteLine($"  ✗ FAILED: {method.Name} parameter count mismatch");
                        Console.WriteLine($"    Expected: {expectedParamCount}");
                        Console.WriteLine($"    Got: {deserialized.GetParameters().Length}");
                        return false;
                    }

                    // Verify method name matches
                    if (deserialized.Name != method.Name)
                    {
                        Console.WriteLine($"  ✗ FAILED: Method name mismatch - expected {method.Name}, got {deserialized.Name}");
                        return false;
                    }

                    // Verify parameter types match
                    var originalParams = method.GetParameters();
                    var deserializedParams = deserialized.GetParameters();

                    for (int i = 0; i < originalParams.Length; i++)
                    {
                        if (originalParams[i].ParameterType.FullName != deserializedParams[i].ParameterType.FullName)
                        {
                            Console.WriteLine($"  ✗ FAILED: {method.Name} parameter {i} type mismatch");
                            Console.WriteLine($"    Expected: {originalParams[i].ParameterType.FullName}");
                            Console.WriteLine($"    Got: {deserializedParams[i].ParameterType.FullName}");
                            return false;
                        }
                    }
                }

                Console.WriteLine("  ✓ PASSED: Complex assembly qualified names parsed correctly");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }
    }
}
