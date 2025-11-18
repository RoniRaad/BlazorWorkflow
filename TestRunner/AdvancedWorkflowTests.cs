using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Helpers;

namespace TestRunner
{
    /// <summary>
    /// Advanced workflow tests covering complex real-world scenarios.
    /// </summary>
    public static class AdvancedWorkflowTests
    {
        public static async Task Run()
        {
            Console.WriteLine("=== Advanced Workflow Integration Tests ===\n");

            bool allPassed = true;
            allPassed &= await TestDataValidationPipeline();
            allPassed &= await TestNullHandlingAndDefaults();
            allPassed &= await TestListSearchAndFind();
            allPassed &= await TestJSONObjectConstruction();
            allPassed &= await TestDataMergingAndJoining();
            allPassed &= await TestAccumulatorPattern();
            allPassed &= await TestMultiOutputBranching();
            allPassed &= await TestStringParsingAndExtraction();
            allPassed &= await TestRangeOperations();
            allPassed &= await TestConditionalAccumulation();
            allPassed &= await TestBatchProcessing();
            allPassed &= await TestErrorRecovery();
            allPassed &= await TestDeepNestedDataAccess();
            allPassed &= await TestDynamicPropertyAccess();
            allPassed &= await TestArrayTransformations();

            Console.WriteLine();
            if (allPassed)
            {
                Console.WriteLine("✓ All advanced workflow tests PASSED");
            }
            else
            {
                Console.WriteLine("✗ Some advanced workflow tests FAILED");
            }

            Console.WriteLine("\n=== End Advanced Workflow Tests ===\n");
        }

        private static async Task<bool> TestDataValidationPipeline()
        {
            Console.WriteLine("[Test 1] Data Validation Pipeline (Validate -> Transform -> Store)");
            try
            {
                // Scenario: Validate user input, transform valid data, reject invalid
                var inputs = new[]
                {
                    new { email = "john@example.com", age = 25 },
                    new { email = "invalid-email", age = 30 },
                    new { email = "jane@test.com", age = -5 },
                    new { email = "bob@example.com", age = 35 }
                };

                var validUsers = new List<object>();
                var errors = new List<string>();

                foreach (var input in inputs)
                {
                    var validationResult = new JsonObject
                    {
                        ["emailValid"] = input.email.Contains("@") && input.email.Contains("."),
                        ["ageValid"] = input.age > 0 && input.age < 150
                    };

                    var plain = validationResult.ToPlainObject() as Dictionary<string, object?>;
                    if (plain == null)
                    {
                        Console.WriteLine("  ✗ FAILED: Validation result conversion");
                        return false;
                    }

                    var emailValid = plain["emailValid"] is bool ev && ev;
                    var ageValid = plain["ageValid"] is bool av && av;

                    if (plain["emailValid"] is not bool || plain["ageValid"] is not bool)
                    {
                        Console.WriteLine("  ✗ FAILED: Validation booleans not properly typed");
                        return false;
                    }

                    if (emailValid && ageValid)
                    {
                        validUsers.Add(input);
                    }
                    else
                    {
                        errors.Add($"Invalid data: email={input.email}, age={input.age}");
                    }
                }

                if (validUsers.Count != 2)
                {
                    Console.WriteLine($"  ✗ FAILED: Expected 2 valid users, got {validUsers.Count}");
                    return false;
                }

                if (errors.Count != 2)
                {
                    Console.WriteLine($"  ✗ FAILED: Expected 2 errors, got {errors.Count}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Data validation pipeline works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestNullHandlingAndDefaults()
        {
            Console.WriteLine("[Test 2] Null Handling and Default Values");
            try
            {
                // Scenario: Handle missing/null values with defaults
                var records = new[]
                {
                    new { name = "Alice", age = (int?)25, city = (string?)"NYC" },
                    new { name = "Bob", age = (int?)null, city = (string?)"LA" },
                    new { name = "Charlie", age = (int?)30, city = (string?)null }
                };

                var processed = new List<Dictionary<string, object?>>();

                foreach (var record in records)
                {
                    var data = new JsonObject
                    {
                        ["name"] = record.name,
                        ["age"] = record.age.HasValue ? JsonValue.Create(record.age.Value) : null,
                        ["city"] = record.city ?? "Unknown"
                    };

                    // Apply defaults for null values
                    var age = data.GetByPath("age");
                    var ageValue = age != null ? age.CoerceToType(typeof(int)) : 0;

                    var city = data.GetByPath("city");
                    var cityValue = city?.CoerceToType(typeof(string)) ?? "Unknown";

                    if (ageValue is not int)
                    {
                        Console.WriteLine($"  ✗ FAILED: Age default should be int, got {ageValue?.GetType().Name}");
                        return false;
                    }

                    if (cityValue is not string)
                    {
                        Console.WriteLine($"  ✗ FAILED: City should be string, got {cityValue?.GetType().Name}");
                        return false;
                    }

                    processed.Add(new Dictionary<string, object?>
                    {
                        ["name"] = record.name,
                        ["age"] = ageValue,
                        ["city"] = cityValue
                    });
                }

                // Verify Bob got age default of 0
                var bob = processed.FirstOrDefault(p => p["name"] as string == "Bob");
                if (bob == null || (int)bob["age"]! != 0)
                {
                    Console.WriteLine("  ✗ FAILED: Bob's age default not applied correctly");
                    return false;
                }

                // Verify Charlie got city default "Unknown"
                var charlie = processed.FirstOrDefault(p => p["name"] as string == "Charlie");
                if (charlie == null || charlie["city"] as string != "Unknown")
                {
                    Console.WriteLine("  ✗ FAILED: Charlie's city default not applied correctly");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Null handling and defaults work");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestListSearchAndFind()
        {
            Console.WriteLine("[Test 3] List Search and Find Operations");
            try
            {
                // Scenario: Find specific items in a list based on criteria
                var products = new[]
                {
                    new { id = 1, name = "Laptop", price = 999.99, inStock = true },
                    new { id = 2, name = "Mouse", price = 29.99, inStock = true },
                    new { id = 3, name = "Keyboard", price = 79.99, inStock = false },
                    new { id = 4, name = "Monitor", price = 299.99, inStock = true }
                };

                // Find first product over $100 that's in stock
                object? foundProduct = null;

                foreach (var product in products)
                {
                    var check = new JsonObject
                    {
                        ["priceCheck"] = product.price > 100,
                        ["stockCheck"] = product.inStock
                    };

                    var plain = check.ToPlainObject() as Dictionary<string, object?>;
                    if (plain?["priceCheck"] is bool priceOk && priceOk &&
                        plain?["stockCheck"] is bool stockOk && stockOk)
                    {
                        foundProduct = product;
                        break; // Found first match
                    }
                }

                if (foundProduct == null)
                {
                    Console.WriteLine("  ✗ FAILED: Should have found a product");
                    return false;
                }

                var found = foundProduct as dynamic;
                if (found?.name != "Laptop")
                {
                    Console.WriteLine($"  ✗ FAILED: Expected Laptop, found {found?.name}");
                    return false;
                }

                // Count products under $100
                var affordableCount = 0;
                foreach (var product in products)
                {
                    var affordableCheck = new JsonObject { ["affordable"] = product.price < 100 };
                    var result = affordableCheck.GetByPath("affordable")?.CoerceToType(typeof(bool));

                    if (result is bool isAffordable && isAffordable)
                    {
                        affordableCount++;
                    }
                }

                if (affordableCount != 2) // Mouse and Keyboard
                {
                    Console.WriteLine($"  ✗ FAILED: Expected 2 affordable products, found {affordableCount}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: List search and find works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestJSONObjectConstruction()
        {
            Console.WriteLine("[Test 4] JSON Object Construction (Building complex objects)");
            try
            {
                // Scenario: Build a complex JSON response from multiple data sources
                var result = new JsonObject();

                // Set basic fields
                result.SetByPath("status", "success");
                result.SetByPath("timestamp", "2024-01-01T12:00:00Z");

                // Build nested user object
                result.SetByPath("data.user.id", 123);
                result.SetByPath("data.user.name", "John Doe");
                result.SetByPath("data.user.email", "john@example.com");

                // Build array of permissions
                var permissions = new JsonArray { "read", "write", "delete" };
                result.SetByPath("data.user.permissions", permissions);

                // Build metadata
                result.SetByPath("metadata.version", 2);
                result.SetByPath("metadata.cached", false);

                // Verify structure
                var status = result.GetByPath("status")?.CoerceToType(typeof(string));
                if (status as string != "success")
                {
                    Console.WriteLine("  ✗ FAILED: Status field incorrect");
                    return false;
                }

                var userId = result.GetByPath("data.user.id")?.CoerceToType(typeof(int));
                if (userId is not int id || id != 123)
                {
                    Console.WriteLine($"  ✗ FAILED: User ID incorrect - got {userId?.GetType().Name}");
                    return false;
                }

                var perms = result.GetByPath("data.user.permissions")?.CoerceToType(typeof(string[]));
                if (perms is not string[] permArray || permArray.Length != 3)
                {
                    Console.WriteLine("  ✗ FAILED: Permissions array incorrect");
                    return false;
                }

                var cached = result.GetByPath("metadata.cached")?.CoerceToType(typeof(bool));
                if (cached is not bool c || c != false)
                {
                    Console.WriteLine($"  ✗ FAILED: Cached flag incorrect - got {cached?.GetType().Name}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: JSON object construction works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestDataMergingAndJoining()
        {
            Console.WriteLine("[Test 5] Data Merging and Joining (Combine multiple sources)");
            try
            {
                // Scenario: Join user data with order data
                var users = new[]
                {
                    new { userId = 1, name = "Alice" },
                    new { userId = 2, name = "Bob" }
                };

                var orders = new[]
                {
                    new { orderId = 101, userId = 1, amount = 50.0 },
                    new { orderId = 102, userId = 1, amount = 75.0 },
                    new { orderId = 103, userId = 2, amount = 100.0 }
                };

                var merged = new List<JsonObject>();

                foreach (var order in orders)
                {
                    var user = users.FirstOrDefault(u => u.userId == order.userId);
                    if (user != null)
                    {
                        var mergedData = new JsonObject
                        {
                            ["orderId"] = order.orderId,
                            ["amount"] = order.amount,
                            ["userName"] = user.name,
                            ["userId"] = user.userId
                        };

                        merged.Add(mergedData);
                    }
                }

                if (merged.Count != 3)
                {
                    Console.WriteLine($"  ✗ FAILED: Expected 3 merged records, got {merged.Count}");
                    return false;
                }

                // Verify first order has correct user name
                var firstOrder = merged[0];
                var userName = firstOrder.GetByPath("userName")?.CoerceToType(typeof(string));
                if (userName as string != "Alice")
                {
                    Console.WriteLine($"  ✗ FAILED: Expected Alice, got {userName}");
                    return false;
                }

                var amount = firstOrder.GetByPath("amount")?.CoerceToType(typeof(double));
                if (amount is not double amt || amt != 50.0)
                {
                    Console.WriteLine($"  ✗ FAILED: Amount incorrect - got {amount?.GetType().Name}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Data merging and joining works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestAccumulatorPattern()
        {
            Console.WriteLine("[Test 6] Accumulator Pattern (Running totals, max/min)");
            try
            {
                // Scenario: Calculate running totals, max, min, count
                var transactions = new[] { 100.0, -50.0, 75.0, -25.0, 200.0 };

                var state = new JsonObject
                {
                    ["balance"] = 0.0,
                    ["maxTransaction"] = double.MinValue,
                    ["minTransaction"] = double.MaxValue,
                    ["transactionCount"] = 0
                };

                foreach (var transaction in transactions)
                {
                    var currentBalance = state.GetByPath("balance")?.CoerceToType(typeof(double));
                    var currentMax = state.GetByPath("maxTransaction")?.CoerceToType(typeof(double));
                    var currentMin = state.GetByPath("minTransaction")?.CoerceToType(typeof(double));
                    var currentCount = state.GetByPath("transactionCount")?.CoerceToType(typeof(int));

                    if (currentBalance is not double balance ||
                        currentMax is not double max ||
                        currentMin is not double min ||
                        currentCount is not int count)
                    {
                        Console.WriteLine("  ✗ FAILED: State value type conversion failed");
                        return false;
                    }

                    // Update accumulator
                    state.SetByPath("balance", balance + transaction);
                    state.SetByPath("maxTransaction", Math.Max(max, transaction));
                    state.SetByPath("minTransaction", Math.Min(min, transaction));
                    state.SetByPath("transactionCount", count + 1);
                }

                var finalBalance = state.GetByPath("balance")?.CoerceToType(typeof(double)) as double?;
                var finalMax = state.GetByPath("maxTransaction")?.CoerceToType(typeof(double)) as double?;
                var finalMin = state.GetByPath("minTransaction")?.CoerceToType(typeof(double)) as double?;
                var finalCount = state.GetByPath("transactionCount")?.CoerceToType(typeof(int)) as int?;

                if (finalBalance != 300.0)
                {
                    Console.WriteLine($"  ✗ FAILED: Final balance should be 300, got {finalBalance}");
                    return false;
                }

                if (finalMax != 200.0)
                {
                    Console.WriteLine($"  ✗ FAILED: Max should be 200, got {finalMax}");
                    return false;
                }

                if (finalMin != -50.0)
                {
                    Console.WriteLine($"  ✗ FAILED: Min should be -50, got {finalMin}");
                    return false;
                }

                if (finalCount != 5)
                {
                    Console.WriteLine($"  ✗ FAILED: Count should be 5, got {finalCount}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Accumulator pattern works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestMultiOutputBranching()
        {
            Console.WriteLine("[Test 7] Multi-Output Branching (Route to different paths)");
            try
            {
                // Scenario: Route items to different processing paths based on category
                var items = new[]
                {
                    new { name = "Laptop", category = "electronics", price = 999.0 },
                    new { name = "Shirt", category = "clothing", price = 29.0 },
                    new { name = "Phone", category = "electronics", price = 699.0 },
                    new { name = "Pants", category = "clothing", price = 59.0 },
                    new { name = "Book", category = "media", price = 15.0 }
                };

                var electronics = new List<string>();
                var clothing = new List<string>();
                var other = new List<string>();

                foreach (var item in items)
                {
                    var categoryCheck = new JsonObject
                    {
                        ["isElectronics"] = item.category == "electronics",
                        ["isClothing"] = item.category == "clothing"
                    };

                    var plain = categoryCheck.ToPlainObject() as Dictionary<string, object?>;

                    if (plain?["isElectronics"] is bool isElec && isElec)
                    {
                        electronics.Add(item.name);
                    }
                    else if (plain?["isClothing"] is bool isCloth && isCloth)
                    {
                        clothing.Add(item.name);
                    }
                    else
                    {
                        other.Add(item.name);
                    }
                }

                if (electronics.Count != 2 || !electronics.Contains("Laptop") || !electronics.Contains("Phone"))
                {
                    Console.WriteLine($"  ✗ FAILED: Electronics routing incorrect");
                    return false;
                }

                if (clothing.Count != 2 || !clothing.Contains("Shirt") || !clothing.Contains("Pants"))
                {
                    Console.WriteLine($"  ✗ FAILED: Clothing routing incorrect");
                    return false;
                }

                if (other.Count != 1 || !other.Contains("Book"))
                {
                    Console.WriteLine($"  ✗ FAILED: Other routing incorrect");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Multi-output branching works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestStringParsingAndExtraction()
        {
            Console.WriteLine("[Test 8] String Parsing and Extraction");
            try
            {
                // Scenario: Parse email addresses and extract domain
                var emails = new[]
                {
                    "alice@example.com",
                    "bob@test.org",
                    "charlie@example.com"
                };

                var domains = new Dictionary<string, int>();

                foreach (var email in emails)
                {
                    var emailData = new JsonObject { ["email"] = email };
                    var emailValue = emailData.GetByPath("email")?.CoerceToType(typeof(string)) as string;

                    if (emailValue == null)
                    {
                        Console.WriteLine("  ✗ FAILED: Email extraction failed");
                        return false;
                    }

                    var parts = emailValue.Split('@');
                    if (parts.Length == 2)
                    {
                        var domain = parts[1];
                        if (domains.ContainsKey(domain))
                        {
                            domains[domain]++;
                        }
                        else
                        {
                            domains[domain] = 1;
                        }
                    }
                }

                if (domains["example.com"] != 2)
                {
                    Console.WriteLine($"  ✗ FAILED: example.com count should be 2, got {domains.GetValueOrDefault("example.com")}");
                    return false;
                }

                if (domains["test.org"] != 1)
                {
                    Console.WriteLine($"  ✗ FAILED: test.org count should be 1, got {domains.GetValueOrDefault("test.org")}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: String parsing and extraction works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestRangeOperations()
        {
            Console.WriteLine("[Test 9] Range Operations (Between, Outside range)");
            try
            {
                // Scenario: Categorize values by range
                var values = new[] { 5, 15, 25, 35, 45, 55, 65, 75 };
                var categories = new Dictionary<string, List<int>>
                {
                    ["low"] = new List<int>(),
                    ["medium"] = new List<int>(),
                    ["high"] = new List<int>()
                };

                foreach (var value in values)
                {
                    var rangeCheck = new JsonObject
                    {
                        ["isLow"] = value < 20,
                        ["isMedium"] = value >= 20 && value < 50,
                        ["isHigh"] = value >= 50
                    };

                    var plain = rangeCheck.ToPlainObject() as Dictionary<string, object?>;

                    if (plain?["isLow"] is bool isLow && isLow)
                    {
                        categories["low"].Add(value);
                    }
                    else if (plain?["isMedium"] is bool isMed && isMed)
                    {
                        categories["medium"].Add(value);
                    }
                    else if (plain?["isHigh"] is bool isHigh && isHigh)
                    {
                        categories["high"].Add(value);
                    }
                }

                if (categories["low"].Count != 2 || !categories["low"].Contains(5) || !categories["low"].Contains(15))
                {
                    Console.WriteLine($"  ✗ FAILED: Low range incorrect");
                    return false;
                }

                if (categories["medium"].Count != 3)
                {
                    Console.WriteLine($"  ✗ FAILED: Medium range incorrect - got {categories["medium"].Count}");
                    return false;
                }

                if (categories["high"].Count != 3)
                {
                    Console.WriteLine($"  ✗ FAILED: High range incorrect - got {categories["high"].Count}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Range operations work");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestConditionalAccumulation()
        {
            Console.WriteLine("[Test 10] Conditional Accumulation (Sum only if condition met)");
            try
            {
                // Scenario: Sum only positive values, count negatives separately
                var numbers = new[] { 10.0, -5.0, 20.0, -3.0, 15.0, -8.0 };
                var positiveSum = 0.0;
                var negativeCount = 0;

                foreach (var number in numbers)
                {
                    var check = new JsonObject { ["isPositive"] = number > 0 };
                    var isPositive = check.GetByPath("isPositive")?.CoerceToType(typeof(bool));

                    if (isPositive is bool pos && pos)
                    {
                        positiveSum += number;
                    }
                    else
                    {
                        negativeCount++;
                    }
                }

                if (Math.Abs(positiveSum - 45.0) > 0.01)
                {
                    Console.WriteLine($"  ✗ FAILED: Positive sum should be 45, got {positiveSum}");
                    return false;
                }

                if (negativeCount != 3)
                {
                    Console.WriteLine($"  ✗ FAILED: Negative count should be 3, got {negativeCount}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Conditional accumulation works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestBatchProcessing()
        {
            Console.WriteLine("[Test 11] Batch Processing (Process in groups)");
            try
            {
                // Scenario: Process items in batches of 3
                var items = Enumerable.Range(1, 10).ToList(); // 1-10
                var batches = new List<List<int>>();
                var currentBatch = new List<int>();

                for (int i = 0; i < items.Count; i++)
                {
                    var iterData = new JsonObject
                    {
                        ["currentIndex"] = i,
                        ["batchSize"] = 3,
                        ["shouldFlush"] = (i + 1) % 3 == 0 || i == items.Count - 1
                    };

                    currentBatch.Add(items[i]);

                    var shouldFlush = iterData.GetByPath("shouldFlush")?.CoerceToType(typeof(bool));
                    if (shouldFlush is bool flush && flush)
                    {
                        batches.Add(new List<int>(currentBatch));
                        currentBatch.Clear();
                    }
                }

                // Should have 4 batches: [1,2,3], [4,5,6], [7,8,9], [10]
                if (batches.Count != 4)
                {
                    Console.WriteLine($"  ✗ FAILED: Expected 4 batches, got {batches.Count}");
                    return false;
                }

                if (batches[0].Count != 3 || batches[1].Count != 3 || batches[2].Count != 3 || batches[3].Count != 1)
                {
                    Console.WriteLine($"  ✗ FAILED: Batch sizes incorrect");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Batch processing works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestErrorRecovery()
        {
            Console.WriteLine("[Test 12] Error Recovery (Fallback values)");
            try
            {
                // Scenario: Try to process data, use fallback on error
                var inputs = new[] { "123", "abc", "456", "xyz", "789" };
                var results = new List<int>();

                foreach (var input in inputs)
                {
                    var parseData = new JsonObject { ["input"] = input };
                    var value = parseData.GetByPath("input")?.CoerceToType(typeof(string)) as string;

                    if (value != null && int.TryParse(value, out int parsed))
                    {
                        results.Add(parsed);
                    }
                    else
                    {
                        // Fallback: use 0
                        results.Add(0);
                    }
                }

                // Should be: 123, 0, 456, 0, 789
                if (results.Count != 5)
                {
                    Console.WriteLine($"  ✗ FAILED: Expected 5 results, got {results.Count}");
                    return false;
                }

                if (results[0] != 123 || results[1] != 0 || results[2] != 456 || results[3] != 0 || results[4] != 789)
                {
                    Console.WriteLine($"  ✗ FAILED: Results incorrect: {string.Join(", ", results)}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Error recovery works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestDeepNestedDataAccess()
        {
            Console.WriteLine("[Test 13] Deep Nested Data Access");
            try
            {
                // Scenario: Access deeply nested JSON structure
                var data = new JsonObject();
                data.SetByPath("company.department.team.member.name", "John");
                data.SetByPath("company.department.team.member.id", 12345);
                data.SetByPath("company.department.team.member.active", true);
                data.SetByPath("company.department.team.budget", 50000.0);

                var name = data.GetByPath("company.department.team.member.name")?.CoerceToType(typeof(string));
                var id = data.GetByPath("company.department.team.member.id")?.CoerceToType(typeof(int));
                var active = data.GetByPath("company.department.team.member.active")?.CoerceToType(typeof(bool));
                var budget = data.GetByPath("company.department.team.budget")?.CoerceToType(typeof(double));

                if (name as string != "John")
                {
                    Console.WriteLine($"  ✗ FAILED: Name should be John, got {name}");
                    return false;
                }

                if (id is not int idValue || idValue != 12345)
                {
                    Console.WriteLine($"  ✗ FAILED: ID incorrect - got {id?.GetType().Name}");
                    return false;
                }

                if (active is not bool activeValue || !activeValue)
                {
                    Console.WriteLine($"  ✗ FAILED: Active flag incorrect - got {active?.GetType().Name}");
                    return false;
                }

                if (budget is not double budgetValue || budgetValue != 50000.0)
                {
                    Console.WriteLine($"  ✗ FAILED: Budget incorrect - got {budget?.GetType().Name}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Deep nested data access works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestDynamicPropertyAccess()
        {
            Console.WriteLine("[Test 14] Dynamic Property Access (Property names from variables)");
            try
            {
                // Scenario: Access properties using dynamic names
                var user = new JsonObject
                {
                    ["firstName"] = "John",
                    ["lastName"] = "Doe",
                    ["age"] = 30,
                    ["email"] = "john@example.com"
                };

                var propertiesToRead = new[] { "firstName", "lastName", "age" };
                var values = new Dictionary<string, object?>();

                foreach (var propName in propertiesToRead)
                {
                    var propData = new JsonObject { ["propertyName"] = propName };
                    var property = propData.GetByPath("propertyName")?.CoerceToType(typeof(string)) as string;

                    if (property != null)
                    {
                        var value = user.GetByPath(property);
                        if (value != null)
                        {
                            // Coerce to appropriate type based on property name
                            if (property == "age")
                            {
                                values[property] = value.CoerceToType(typeof(int));
                            }
                            else
                            {
                                values[property] = value.CoerceToType(typeof(string));
                            }
                        }
                    }
                }

                if (values.Count != 3)
                {
                    Console.WriteLine($"  ✗ FAILED: Expected 3 values, got {values.Count}");
                    return false;
                }

                if (values["firstName"] as string != "John")
                {
                    Console.WriteLine($"  ✗ FAILED: firstName incorrect - got type {values["firstName"]?.GetType().Name}, value {values["firstName"]}");
                    return false;
                }

                if (values["lastName"] as string != "Doe")
                {
                    Console.WriteLine($"  ✗ FAILED: lastName incorrect - got type {values["lastName"]?.GetType().Name}, value {values["lastName"]}");
                    return false;
                }

                if (values["age"] is not int age || age != 30)
                {
                    Console.WriteLine($"  ✗ FAILED: age incorrect - got {values["age"]?.GetType().Name}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Dynamic property access works");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ FAILED: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TestArrayTransformations()
        {
            Console.WriteLine("[Test 15] Array Transformations (Map, Filter, Reduce patterns)");
            try
            {
                // Scenario: Transform array - filter evens, square them, sum the result
                var numbers = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

                // Step 1: Filter evens
                var evens = new List<int>();
                foreach (var num in numbers)
                {
                    var check = new JsonObject { ["isEven"] = num % 2 == 0 };
                    var isEven = check.GetByPath("isEven")?.CoerceToType(typeof(bool));

                    if (isEven is bool even && even)
                    {
                        evens.Add(num);
                    }
                }

                // Step 2: Map - square each
                var squared = new List<int>();
                foreach (var num in evens)
                {
                    var calculation = new JsonObject
                    {
                        ["value"] = num,
                        ["squared"] = num * num
                    };

                    var result = calculation.GetByPath("squared")?.CoerceToType(typeof(int));
                    if (result is int sq)
                    {
                        squared.Add(sq);
                    }
                }

                // Step 3: Reduce - sum all
                var sum = 0;
                foreach (var value in squared)
                {
                    var accumulator = new JsonObject
                    {
                        ["currentSum"] = sum,
                        ["addValue"] = value
                    };

                    var currentSum = accumulator.GetByPath("currentSum")?.CoerceToType(typeof(int));
                    var addValue = accumulator.GetByPath("addValue")?.CoerceToType(typeof(int));

                    if (currentSum is int current && addValue is int add)
                    {
                        sum = current + add;
                    }
                }

                // evens: 2, 4, 6, 8, 10
                // squared: 4, 16, 36, 64, 100
                // sum: 220
                if (sum != 220)
                {
                    Console.WriteLine($"  ✗ FAILED: Sum should be 220, got {sum}");
                    return false;
                }

                Console.WriteLine("  ✓ PASSED: Array transformations work");
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
