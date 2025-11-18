using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Drawflow.BaseNodes;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models.NodeV2;

namespace TestRunner
{
    /// <summary>
    /// Integration tests simulating common real-world workflow scenarios.
    /// These tests catch issues that only appear when nodes are connected and used together.
    /// </summary>
    public static class CommonWorkflowTests
    {
        public static async Task Run()
        {
            Console.WriteLine("=== Common Workflow Integration Tests ===\n");

            bool allPassed = true;
            allPassed &= await TestSimpleDataTransformation();
            allPassed &= await TestConditionalBranching();
            allPassed &= await TestIterationWithTransformation();
            allPassed &= await TestFilteringAndMapping();
            allPassed &= await TestNestedIterations();
            allPassed &= await TestStringManipulation();
            allPassed &= await TestMathOperations();
            allPassed &= await TestDataAggregation();
            allPassed &= await TestComplexConditionals();
            allPassed &= await TestVariableStorageAndRetrieval();

            Console.WriteLine();
            if (allPassed)
            {
                Console.WriteLine("✓ All common workflow tests PASSED");
            }
            else
            {
                Console.WriteLine("✗ Some common workflow tests FAILED");
            }

            Console.WriteLine("\n=== End Common Workflow Tests ===\n");
        }

        private static async Task<bool> TestSimpleDataTransformation()
        {
            Console.WriteLine("[Test 1] Simple Data Transformation (Input -> Transform -> Output)");
            try
            {
                // Scenario: User wants to create an object and extract a field
                var inputData = new JsonObject
                {
                    ["user"] = new JsonObject
                    {
                        ["name"] = "John Doe",
                        ["age"] = 30,
                        ["email"] = "john@example.com"
                    }
                };

                // Get nested value using GetByPath (what templates do)
                var name = inputData.GetByPath("user.name");
                var age = inputData.GetByPath("user.age");

                if (name?.CoerceToType(typeof(string)) as string != "John Doe")
                {
                    Console.WriteLine("  ✗ FAILED: Name extraction failed");
                    return false;
                }

                if (age?.CoerceToType(typeof(int)) is not int ageValue || ageValue != 30)
                {
                    Console.WriteLine("  ✗ FAILED: Age extraction failed");
                    return false;
                }

                // Set new values using SetByPath
                inputData.SetByPath("user.verified", true);
                inputData.SetByPath("user.metadata.source", "api");

                var verified = inputData.GetByPath("user.verified");
                if (verified?.CoerceToType(typeof(bool)) is not bool verifiedValue || !verifiedValue)
                {
                    Console.WriteLine("  ✗ FAILED: Boolean field setting failed");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Data transformation works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestConditionalBranching()
        {
            Console.WriteLine("[Test 2] Conditional Branching (If node with comparison)");
            try
            {
                // Scenario: Check if user is adult and branch accordingly
                var users = new[]
                {
                    new { name = "Alice", age = 25 },
                    new { name = "Bob", age = 17 },
                    new { name = "Charlie", age = 30 }
                };

                var adults = new List<string>();
                var minors = new List<string>();

                foreach (var user in users)
                {
                    // Simulate GreaterThanOrEqual node
                    var comparisonResult = new JsonObject
                    {
                        ["output"] = new JsonObject { ["result"] = user.age >= 18 }
                    };

                    var plainResult = comparisonResult.ToPlainObject();
                    if (plainResult is Dictionary<string, object?> dict &&
                        dict["output"] is Dictionary<string, object?> output)
                    {
                        var result = output["result"];
                        if (result is not bool isAdult)
                        {
                            Console.WriteLine($"  ✗ FAILED: Boolean result not properly unwrapped - got {result?.GetType().Name}");
                            return false;
                        }

                        // Simulate If node branching
                        if (isAdult)
                        {
                            adults.Add(user.name);
                        }
                        else
                        {
                            minors.Add(user.name);
                        }
                    }
                }

                if (adults.Count != 2 || !adults.Contains("Alice") || !adults.Contains("Charlie"))
                {
                    Console.WriteLine("  ✗ FAILED: Adult filtering incorrect");
                    return false;
                }

                if (minors.Count != 1 || !minors.Contains("Bob"))
                {
                    Console.WriteLine("  ✗ FAILED: Minor filtering incorrect");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Conditional branching works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestIterationWithTransformation()
        {
            Console.WriteLine("[Test 3] Iteration with Transformation (ForEach -> Transform -> Collect)");
            try
            {
                // Scenario: Process a list of names, uppercase them, and collect results
                var names = new List<string> { "alice", "bob", "charlie" };
                var results = new List<string>();

                // Simulate ForEachString iteration
                for (int i = 0; i < names.Count; i++)
                {
                    var iterationOutput = new JsonObject
                    {
                        ["output"] = new JsonObject
                        {
                            ["currentItem"] = JsonSerializer.SerializeToNode(names[i]),
                            ["currentIndex"] = i,
                            ["isFirst"] = i == 0,
                            ["isLast"] = i == names.Count - 1
                        }
                    };

                    // Convert to plain object (what templates receive)
                    var plain = iterationOutput.ToPlainObject();
                    if (plain is Dictionary<string, object?> dict &&
                        dict["output"] is Dictionary<string, object?> output)
                    {
                        var currentItem = output["currentItem"];
                        var currentIndex = output["currentIndex"];
                        var isLast = output["isLast"];

                        // Verify types are correct
                        if (currentItem is not string name)
                        {
                            Console.WriteLine($"  ✗ FAILED: currentItem should be string, got {currentItem?.GetType().Name}");
                            return false;
                        }

                        if (currentIndex is not int index)
                        {
                            Console.WriteLine($"  ✗ FAILED: currentIndex should be int, got {currentIndex?.GetType().Name}");
                            return false;
                        }

                        if (isLast is not bool last)
                        {
                            Console.WriteLine($"  ✗ FAILED: isLast should be bool, got {isLast?.GetType().Name}");
                            return false;
                        }

                        // Transform the data
                        results.Add(name.ToUpper());
                    }
                }

                if (results.Count != 3 ||
                    results[0] != "ALICE" ||
                    results[1] != "BOB" ||
                    results[2] != "CHARLIE")
                {
                    Console.WriteLine("  ✗ FAILED: Transformation results incorrect");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Iteration with transformation works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestFilteringAndMapping()
        {
            Console.WriteLine("[Test 4] Filtering and Mapping (Filter -> Map -> Collect)");
            try
            {
                // Scenario: Filter numbers > 10, then double them
                var numbers = new List<double> { 5, 12, 8, 15, 20, 3 };
                var results = new List<double>();

                foreach (var number in numbers)
                {
                    // Filter: number > 10
                    var filterResult = new JsonObject
                    {
                        ["output"] = new JsonObject { ["result"] = number > 10 }
                    };

                    var shouldInclude = filterResult.GetByPath("output.result")?.CoerceToType(typeof(bool));
                    if (shouldInclude is bool include && include)
                    {
                        // Map: double the value
                        var mapped = number * 2;
                        results.Add(mapped);
                    }
                }

                var expected = new List<double> { 24, 30, 40 }; // 12*2, 15*2, 20*2
                if (results.Count != expected.Count || !results.SequenceEqual(expected))
                {
                    Console.WriteLine($"  ✗ FAILED: Expected {string.Join(", ", expected)}, got {string.Join(", ", results)}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Filtering and mapping works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestNestedIterations()
        {
            Console.WriteLine("[Test 5] Nested Iterations (ForEach inside ForEach)");
            try
            {
                // Scenario: Matrix multiplication preparation - iterate rows, then columns
                var matrix = new List<List<int>>
                {
                    new List<int> { 1, 2, 3 },
                    new List<int> { 4, 5, 6 }
                };

                var results = new List<string>();

                // Outer loop: rows
                for (int rowIndex = 0; rowIndex < matrix.Count; rowIndex++)
                {
                    var row = matrix[rowIndex];

                    // Inner loop: columns
                    for (int colIndex = 0; colIndex < row.Count; colIndex++)
                    {
                        var value = row[colIndex];

                        // Create nested iteration output
                        var output = new JsonObject
                        {
                            ["output"] = new JsonObject
                            {
                                ["rowIndex"] = rowIndex,
                                ["colIndex"] = colIndex,
                                ["value"] = value
                            }
                        };

                        var plain = output.ToPlainObject();
                        if (plain is Dictionary<string, object?> dict &&
                            dict["output"] is Dictionary<string, object?> outputDict)
                        {
                            var r = outputDict["rowIndex"];
                            var c = outputDict["colIndex"];
                            var v = outputDict["value"];

                            if (r is not int || c is not int || v is not int)
                            {
                                Console.WriteLine($"  ✗ FAILED: Type mismatch in nested iteration - r:{r?.GetType().Name}, c:{c?.GetType().Name}, v:{v?.GetType().Name}");
                                return false;
                            }

                            results.Add($"[{r},{c}]={v}");
                        }
                    }
                }

                if (results.Count != 6)
                {
                    Console.WriteLine($"  ✗ FAILED: Expected 6 results, got {results.Count}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Nested iterations work");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestStringManipulation()
        {
            Console.WriteLine("[Test 6] String Manipulation (Concatenation, Templates)");
            try
            {
                // Scenario: Build email from user data using templates
                var users = new[]
                {
                    new { firstName = "John", lastName = "Doe", domain = "example.com" },
                    new { firstName = "Jane", lastName = "Smith", domain = "test.com" }
                };

                var emails = new List<string>();

                foreach (var user in users)
                {
                    var userData = new JsonObject
                    {
                        ["firstName"] = user.firstName,
                        ["lastName"] = user.lastName,
                        ["domain"] = user.domain
                    };

                    // Simulate template: {{firstName}}.{{lastName}}@{{domain}}
                    var firstName = userData.GetByPath("firstName")?.CoerceToType(typeof(string)) as string;
                    var lastName = userData.GetByPath("lastName")?.CoerceToType(typeof(string)) as string;
                    var domain = userData.GetByPath("domain")?.CoerceToType(typeof(string)) as string;

                    if (firstName == null || lastName == null || domain == null)
                    {
                        Console.WriteLine("  ✗ FAILED: Failed to extract string values");
                        return false;
                    }

                    var email = $"{firstName}.{lastName}@{domain}".ToLower();
                    emails.Add(email);
                }

                if (emails[0] != "john.doe@example.com" || emails[1] != "jane.smith@test.com")
                {
                    Console.WriteLine($"  ✗ FAILED: Email generation incorrect: {string.Join(", ", emails)}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: String manipulation works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestMathOperations()
        {
            Console.WriteLine("[Test 7] Math Operations (Add, Multiply, Divide)");
            try
            {
                // Scenario: Calculate total price with tax
                var items = new[]
                {
                    new { name = "Item1", price = 100.0, quantity = 2 },
                    new { name = "Item2", price = 50.0, quantity = 3 }
                };

                var taxRate = 0.1; // 10%
                var totals = new List<double>();

                foreach (var item in items)
                {
                    var itemData = new JsonObject
                    {
                        ["price"] = item.price,
                        ["quantity"] = item.quantity,
                        ["taxRate"] = taxRate
                    };

                    var price = itemData.GetByPath("price")?.CoerceToType(typeof(double));
                    var quantity = itemData.GetByPath("quantity")?.CoerceToType(typeof(int));
                    var tax = itemData.GetByPath("taxRate")?.CoerceToType(typeof(double));

                    if (price is not double p || quantity is not int q || tax is not double t)
                    {
                        Console.WriteLine($"  ✗ FAILED: Type conversion - price:{price?.GetType().Name}, quantity:{quantity?.GetType().Name}, tax:{tax?.GetType().Name}");
                        return false;
                    }

                    var subtotal = p * q;
                    var taxAmount = subtotal * t;
                    var total = subtotal + taxAmount;

                    totals.Add(total);
                }

                // Item1: 100 * 2 = 200, tax = 20, total = 220
                // Item2: 50 * 3 = 150, tax = 15, total = 165
                if (Math.Abs(totals[0] - 220.0) > 0.01 || Math.Abs(totals[1] - 165.0) > 0.01)
                {
                    Console.WriteLine($"  ✗ FAILED: Math calculations incorrect: {string.Join(", ", totals)}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Math operations work");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestDataAggregation()
        {
            Console.WriteLine("[Test 8] Data Aggregation (Sum, Count, Average)");
            try
            {
                // Scenario: Calculate statistics from a list of numbers
                var numbers = new List<double> { 10, 20, 30, 40, 50 };
                var sum = 0.0;
                var count = 0;

                foreach (var number in numbers)
                {
                    var iterData = new JsonObject
                    {
                        ["currentValue"] = number,
                        ["runningSum"] = sum
                    };

                    var current = iterData.GetByPath("currentValue")?.CoerceToType(typeof(double));
                    if (current is not double val)
                    {
                        Console.WriteLine($"  ✗ FAILED: Number conversion failed - got {current?.GetType().Name}");
                        return false;
                    }

                    sum += val;
                    count++;
                }

                var average = sum / count;

                if (Math.Abs(sum - 150.0) > 0.01)
                {
                    Console.WriteLine($"  ✗ FAILED: Sum incorrect - expected 150, got {sum}");
                    return false;
                }

                if (count != 5)
                {
                    Console.WriteLine($"  ✗ FAILED: Count incorrect - expected 5, got {count}");
                    return false;
                }

                if (Math.Abs(average - 30.0) > 0.01)
                {
                    Console.WriteLine($"  ✗ FAILED: Average incorrect - expected 30, got {average}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Data aggregation works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestComplexConditionals()
        {
            Console.WriteLine("[Test 9] Complex Conditionals (Multiple AND/OR conditions)");
            try
            {
                // Scenario: Filter users based on multiple conditions
                var users = new[]
                {
                    new { name = "Alice", age = 25, premium = true, country = "US" },
                    new { name = "Bob", age = 17, premium = true, country = "US" },
                    new { name = "Charlie", age = 30, premium = false, country = "UK" },
                    new { name = "Diana", age = 28, premium = true, country = "US" }
                };

                var qualified = new List<string>();

                // Rule: (age >= 18 AND premium = true AND country = "US")
                foreach (var user in users)
                {
                    var conditions = new JsonObject
                    {
                        ["ageCheck"] = user.age >= 18,
                        ["premiumCheck"] = user.premium,
                        ["countryCheck"] = user.country == "US"
                    };

                    var plain = conditions.ToPlainObject();
                    if (plain is Dictionary<string, object?> dict)
                    {
                        var ageOk = dict["ageCheck"] is bool a && a;
                        var premiumOk = dict["premiumCheck"] is bool p && p;
                        var countryOk = dict["countryCheck"] is bool c && c;

                        if (!ageOk || !premiumOk || !countryOk)
                        {
                            // Check that at least the types are correct even if values are false
                            if (dict["ageCheck"] is not bool ||
                                dict["premiumCheck"] is not bool ||
                                dict["countryCheck"] is not bool)
                            {
                                Console.WriteLine("  ✗ FAILED: Boolean conditions not properly typed");
                                return false;
                            }
                        }

                        if (ageOk && premiumOk && countryOk)
                        {
                            qualified.Add(user.name);
                        }
                    }
                }

                // Only Alice and Diana should qualify
                if (qualified.Count != 2 || !qualified.Contains("Alice") || !qualified.Contains("Diana"))
                {
                    Console.WriteLine($"  ✗ FAILED: Expected Alice and Diana, got {string.Join(", ", qualified)}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Complex conditionals work");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestVariableStorageAndRetrieval()
        {
            Console.WriteLine("[Test 10] Variable Storage and Retrieval (Set/Get workflow variables)");
            try
            {
                // Scenario: Store intermediate results and use them later
                var workflowState = new JsonObject();

                // Step 1: Calculate and store user count
                var users = new[] { "Alice", "Bob", "Charlie" };
                workflowState.SetByPath("userCount", users.Length);

                // Step 2: Store processed data
                workflowState.SetByPath("processedUsers", JsonSerializer.SerializeToNode(users));

                // Step 3: Store metadata
                workflowState.SetByPath("metadata.processedAt", "2024-01-01");
                workflowState.SetByPath("metadata.success", true);

                // Now retrieve and verify
                var userCount = workflowState.GetByPath("userCount")?.CoerceToType(typeof(int));
                if (userCount is not int count || count != 3)
                {
                    Console.WriteLine($"  ✗ FAILED: userCount retrieval - got {userCount?.GetType().Name} = {userCount}");
                    return false;
                }

                var processedUsers = workflowState.GetByPath("processedUsers")?.CoerceToType(typeof(string[]));
                if (processedUsers is not string[] userArray || userArray.Length != 3)
                {
                    Console.WriteLine($"  ✗ FAILED: processedUsers retrieval - got {processedUsers?.GetType().Name}");
                    return false;
                }

                var processedAt = workflowState.GetByPath("metadata.processedAt")?.CoerceToType(typeof(string));
                if (processedAt is not string date || date != "2024-01-01")
                {
                    Console.WriteLine($"  ✗ FAILED: nested metadata retrieval - got {processedAt}");
                    return false;
                }

                var success = workflowState.GetByPath("metadata.success")?.CoerceToType(typeof(bool));
                if (success is not bool s || !s)
                {
                    Console.WriteLine($"  ✗ FAILED: boolean metadata retrieval - got {success?.GetType().Name} = {success}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Variable storage and retrieval works");
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
