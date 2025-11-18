using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Helpers;

namespace TestRunner
{
    /// <summary>
    /// Comprehensive tests for JsonNode type handling to catch serialization/conversion issues.
    /// These tests ensure that all assignment methods produce values that can be safely
    /// converted to plain objects and used in Scriban templates.
    /// </summary>
    public static class JsonTypeHandlingTest
    {
        public static void Run()
        {
            Console.WriteLine("=== JSON Type Handling Test ===\n");

            bool allPassed = true;
            allPassed &= TestDirectPrimitiveAssignments();
            allPassed &= TestSerializedAssignments();
            allPassed &= TestMixedAssignments();
            allPassed &= TestArrayHandling();
            allPassed &= TestNestedObjects();
            allPassed &= TestEdgeCases();
            allPassed &= TestTemplateAccess();

            Console.WriteLine();
            if (allPassed)
            {
                Console.WriteLine("✓ All JSON type handling tests PASSED");
            }
            else
            {
                Console.WriteLine("✗ Some JSON type handling tests FAILED");
            }

            Console.WriteLine("\n=== End JSON Type Handling Test ===\n");
        }

        private static bool TestDirectPrimitiveAssignments()
        {
            Console.WriteLine("[Test 1] Direct Primitive Assignments");
            try
            {
                var obj = new JsonObject
                {
                    ["int"] = 42,
                    ["long"] = 123456789L,
                    ["double"] = 3.14,
                    ["float"] = 2.71f,
                    ["bool"] = true,
                    ["string"] = "test",
                    ["null"] = null
                };

                var plain = obj.ToPlainObject();
                if (plain is not Dictionary<string, object?> dict)
                {
                    Console.WriteLine("  ✗ FAILED: ToPlainObject didn't return Dictionary");
                    return false;
                }

                // Verify each type
                if (dict["int"] is not int || (int)dict["int"]! != 42)
                {
                    Console.WriteLine($"  ✗ FAILED: int type - got {dict["int"]?.GetType().Name}");
                    return false;
                }

                if (dict["long"] is not long || (long)dict["long"]! != 123456789L)
                {
                    Console.WriteLine($"  ✗ FAILED: long type - got {dict["long"]?.GetType().Name}");
                    return false;
                }

                if (dict["double"] is not double)
                {
                    Console.WriteLine($"  ✗ FAILED: double type - got {dict["double"]?.GetType().Name}");
                    return false;
                }

                if (dict["bool"] is not bool || (bool)dict["bool"]! != true)
                {
                    Console.WriteLine($"  ✗ FAILED: bool type - got {dict["bool"]?.GetType().Name}");
                    return false;
                }

                if (dict["string"] is not string || (string)dict["string"]! != "test")
                {
                    Console.WriteLine($"  ✗ FAILED: string type - got {dict["string"]?.GetType().Name}");
                    return false;
                }

                if (dict["null"] is not null)
                {
                    Console.WriteLine("  ✗ FAILED: null handling");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: All primitive types converted correctly");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static bool TestSerializedAssignments()
        {
            Console.WriteLine("[Test 2] Serialized Assignments (JsonSerializer.SerializeToNode)");
            try
            {
                var obj = new JsonObject
                {
                    ["int"] = JsonSerializer.SerializeToNode(42),
                    ["long"] = JsonSerializer.SerializeToNode(123456789L),
                    ["double"] = JsonSerializer.SerializeToNode(3.14),
                    ["bool"] = JsonSerializer.SerializeToNode(true),
                    ["string"] = JsonSerializer.SerializeToNode("test"),
                    ["object"] = JsonSerializer.SerializeToNode(new { name = "test", value = 123 })
                };

                var plain = obj.ToPlainObject();
                if (plain is not Dictionary<string, object?> dict)
                {
                    Console.WriteLine("  ✗ FAILED: ToPlainObject didn't return Dictionary");
                    return false;
                }

                // Verify types are properly unwrapped
                if (dict["int"] is not int || (int)dict["int"]! != 42)
                {
                    Console.WriteLine($"  ✗ FAILED: serialized int - got {dict["int"]?.GetType().Name}");
                    return false;
                }

                if (dict["bool"] is not bool || (bool)dict["bool"]! != true)
                {
                    Console.WriteLine($"  ✗ FAILED: serialized bool - got {dict["bool"]?.GetType().Name}");
                    return false;
                }

                if (dict["string"] is not string || (string)dict["string"]! != "test")
                {
                    Console.WriteLine($"  ✗ FAILED: serialized string - got {dict["string"]?.GetType().Name}");
                    return false;
                }

                if (dict["object"] is not Dictionary<string, object?>)
                {
                    Console.WriteLine($"  ✗ FAILED: serialized object - got {dict["object"]?.GetType().Name}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Serialized values converted correctly");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static bool TestMixedAssignments()
        {
            Console.WriteLine("[Test 3] Mixed Assignment Methods (Real-world scenario)");
            try
            {
                // This simulates what ForEachString does
                var obj = new JsonObject
                {
                    ["currentItem"] = JsonSerializer.SerializeToNode("test string"),  // Serialized
                    ["currentIndex"] = 5,  // Direct
                    ["totalCount"] = 10,  // Direct
                    ["isFirst"] = false,  // Direct
                    ["isLast"] = false,  // Direct
                    ["metadata"] = JsonSerializer.SerializeToNode(new { source = "test" })  // Serialized
                };

                var plain = obj.ToPlainObject();
                if (plain is not Dictionary<string, object?> dict)
                {
                    Console.WriteLine("  ✗ FAILED: ToPlainObject didn't return Dictionary");
                    return false;
                }

                // All should be proper CLR types
                if (dict["currentItem"] is not string || (string)dict["currentItem"]! != "test string")
                {
                    Console.WriteLine($"  ✗ FAILED: currentItem - got {dict["currentItem"]?.GetType().Name}");
                    return false;
                }

                if (dict["currentIndex"] is not int || (int)dict["currentIndex"]! != 5)
                {
                    Console.WriteLine($"  ✗ FAILED: currentIndex - got {dict["currentIndex"]?.GetType().Name}");
                    return false;
                }

                if (dict["isFirst"] is not bool || (bool)dict["isFirst"]! != false)
                {
                    Console.WriteLine($"  ✗ FAILED: isFirst - got {dict["isFirst"]?.GetType().Name}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Mixed assignments work correctly");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static bool TestArrayHandling()
        {
            Console.WriteLine("[Test 4] Array Handling");
            try
            {
                var directArray = new JsonArray { 1, 2, 3, 4, 5 };
                var serializedArray = JsonSerializer.SerializeToNode(new[] { 1, 2, 3, 4, 5 });

                var directPlain = directArray.ToPlainObject();
                var serializedPlain = serializedArray.ToPlainObject();

                if (directPlain is not List<object?> directList)
                {
                    Console.WriteLine($"  ✗ FAILED: Direct array - got {directPlain?.GetType().Name}");
                    return false;
                }

                if (serializedPlain is not List<object?> serializedList)
                {
                    Console.WriteLine($"  ✗ FAILED: Serialized array - got {serializedPlain?.GetType().Name}");
                    return false;
                }

                // Check all elements are ints
                if (directList.Any(item => item is not int))
                {
                    Console.WriteLine("  ✗ FAILED: Direct array contains non-int elements");
                    return false;
                }

                if (serializedList.Any(item => item is not int))
                {
                    Console.WriteLine("  ✗ FAILED: Serialized array contains non-int elements");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Arrays handled correctly");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static bool TestNestedObjects()
        {
            Console.WriteLine("[Test 5] Nested Object Handling");
            try
            {
                var obj = new JsonObject
                {
                    ["user"] = new JsonObject
                    {
                        ["name"] = "John",
                        ["age"] = 30,
                        ["active"] = true,
                        ["metadata"] = new JsonObject
                        {
                            ["created"] = "2024-01-01",
                            ["count"] = 5
                        }
                    }
                };

                var plain = obj.ToPlainObject();
                if (plain is not Dictionary<string, object?> dict)
                {
                    Console.WriteLine("  ✗ FAILED: Root object conversion");
                    return false;
                }

                if (dict["user"] is not Dictionary<string, object?> user)
                {
                    Console.WriteLine($"  ✗ FAILED: Nested user object - got {dict["user"]?.GetType().Name}");
                    return false;
                }

                if (user["name"] is not string || (string)user["name"]! != "John")
                {
                    Console.WriteLine($"  ✗ FAILED: Nested string - got {user["name"]?.GetType().Name}");
                    return false;
                }

                if (user["age"] is not int || (int)user["age"]! != 30)
                {
                    Console.WriteLine($"  ✗ FAILED: Nested int - got {user["age"]?.GetType().Name}");
                    return false;
                }

                if (user["active"] is not bool || (bool)user["active"]! != true)
                {
                    Console.WriteLine($"  ✗ FAILED: Nested bool - got {user["active"]?.GetType().Name}");
                    return false;
                }

                if (user["metadata"] is not Dictionary<string, object?> metadata)
                {
                    Console.WriteLine($"  ✗ FAILED: Double-nested object - got {user["metadata"]?.GetType().Name}");
                    return false;
                }

                if (metadata["count"] is not int || (int)metadata["count"]! != 5)
                {
                    Console.WriteLine($"  ✗ FAILED: Double-nested int - got {metadata["count"]?.GetType().Name}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Nested objects handled correctly");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static bool TestEdgeCases()
        {
            Console.WriteLine("[Test 6] Edge Cases");
            try
            {
                var obj = new JsonObject
                {
                    ["emptyString"] = "",
                    ["zero"] = 0,
                    ["false"] = false,
                    ["emptyArray"] = new JsonArray(),
                    ["emptyObject"] = new JsonObject(),
                    ["maxInt"] = int.MaxValue,
                    ["minInt"] = int.MinValue,
                    ["maxLong"] = long.MaxValue,
                    ["infinity"] = double.PositiveInfinity,
                    ["negativeZero"] = -0.0
                };

                var plain = obj.ToPlainObject();
                if (plain is not Dictionary<string, object?> dict)
                {
                    Console.WriteLine("  ✗ FAILED: Edge cases object conversion");
                    return false;
                }

                if (dict["emptyString"] is not string || (string)dict["emptyString"]! != "")
                {
                    Console.WriteLine("  ✗ FAILED: Empty string handling");
                    return false;
                }

                if (dict["zero"] is not int || (int)dict["zero"]! != 0)
                {
                    Console.WriteLine("  ✗ FAILED: Zero handling");
                    return false;
                }

                if (dict["false"] is not bool || (bool)dict["false"]! != false)
                {
                    Console.WriteLine("  ✗ FAILED: False handling");
                    return false;
                }

                if (dict["emptyArray"] is not List<object?>)
                {
                    Console.WriteLine("  ✗ FAILED: Empty array handling");
                    return false;
                }

                if (dict["emptyObject"] is not Dictionary<string, object?>)
                {
                    Console.WriteLine("  ✗ FAILED: Empty object handling");
                    return false;
                }

                if (dict["maxInt"] is not int || (int)dict["maxInt"]! != int.MaxValue)
                {
                    Console.WriteLine("  ✗ FAILED: Max int handling");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Edge cases handled correctly");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static bool TestTemplateAccess()
        {
            Console.WriteLine("[Test 7] Template Access Patterns (GetByPath + CoerceToType)");
            try
            {
                var root = new JsonObject
                {
                    ["output"] = new JsonObject
                    {
                        ["currentItem"] = JsonSerializer.SerializeToNode("test"),
                        ["currentIndex"] = 5,
                        ["isFirst"] = false,
                        ["metadata"] = new JsonObject
                        {
                            ["count"] = 10
                        }
                    }
                };

                // Test GetByPath
                var currentItem = root.GetByPath("output.currentItem");
                var currentIndex = root.GetByPath("output.currentIndex");
                var isFirst = root.GetByPath("output.isFirst");
                var nestedCount = root.GetByPath("output.metadata.count");

                if (currentItem == null || currentIndex == null || isFirst == null || nestedCount == null)
                {
                    Console.WriteLine("  ✗ FAILED: GetByPath returned null");
                    return false;
                }

                // Test CoerceToType (what templates use)
                var stringValue = currentItem.CoerceToType(typeof(string));
                var intValue = currentIndex.CoerceToType(typeof(int));
                var boolValue = isFirst.CoerceToType(typeof(bool));
                var nestedIntValue = nestedCount.CoerceToType(typeof(int));

                if (stringValue is not string || (string)stringValue != "test")
                {
                    Console.WriteLine($"  ✗ FAILED: String coercion - got {stringValue?.GetType().Name}");
                    return false;
                }

                if (intValue is not int || (int)intValue != 5)
                {
                    Console.WriteLine($"  ✗ FAILED: Int coercion - got {intValue?.GetType().Name}");
                    return false;
                }

                if (boolValue is not bool || (bool)boolValue != false)
                {
                    Console.WriteLine($"  ✗ FAILED: Bool coercion - got {boolValue?.GetType().Name}");
                    return false;
                }

                if (nestedIntValue is not int || (int)nestedIntValue != 10)
                {
                    Console.WriteLine($"  ✗ FAILED: Nested int coercion - got {nestedIntValue?.GetType().Name}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Template access patterns work correctly");
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
