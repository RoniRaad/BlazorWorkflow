using System.Text.Json;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models.NodeV2;
using Xunit;

namespace TestRunner
{
    /// <summary>
    /// Integration tests simulating common real-world workflow scenarios.
    /// These tests catch issues that only appear when nodes are connected and used together.
    /// </summary>
    public class CommonWorkflowTests
    {
        [Fact]
        public async Task TestSimpleDataTransformation()
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

            Assert.Equal("John Doe", name?.CoerceToType(typeof(string)) as string);
            Assert.Equal(30, age?.CoerceToType(typeof(int)) as int?);

            // Set new values using SetByPath
            inputData.SetByPath("user.verified", true);
            inputData.SetByPath("user.metadata.source", "api");

            var verified = inputData.GetByPath("user.verified");
            Assert.True(verified?.CoerceToType(typeof(bool)) as bool?);
        }

        [Fact]
        public async Task TestConditionalBranching()
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
                    Assert.IsType<bool>(result);
                    var isAdult = (bool)result;

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

            Assert.Equal(2, adults.Count);
            Assert.Contains("Alice", adults);
            Assert.Contains("Charlie", adults);
            Assert.Single(minors);
            Assert.Contains("Bob", minors);
        }

        [Fact]
        public async Task TestIterationWithTransformation()
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
                    Assert.IsType<string>(currentItem);
                    Assert.IsType<int>(currentIndex);
                    Assert.IsType<bool>(isLast);

                    // Transform the data
                    results.Add(((string)currentItem).ToUpper());
                }
            }

            Assert.Equal(3, results.Count);
            Assert.Equal("ALICE", results[0]);
            Assert.Equal("BOB", results[1]);
            Assert.Equal("CHARLIE", results[2]);
        }

        [Fact]
        public async Task TestFilteringAndMapping()
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
            Assert.Equal(expected, results);
        }

        [Fact]
        public async Task TestNestedIterations()
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

                        Assert.IsType<int>(r);
                        Assert.IsType<int>(c);
                        Assert.IsType<int>(v);

                        results.Add($"[{r},{c}]={v}");
                    }
                }
            }

            Assert.Equal(6, results.Count);
        }

        [Fact]
        public async Task TestStringManipulation()
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

                Assert.NotNull(firstName);
                Assert.NotNull(lastName);
                Assert.NotNull(domain);

                var email = $"{firstName}.{lastName}@{domain}".ToLower();
                emails.Add(email);
            }

            Assert.Equal("john.doe@example.com", emails[0]);
            Assert.Equal("jane.smith@test.com", emails[1]);
        }

        [Fact]
        public async Task TestMathOperations()
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

                Assert.IsType<double>(price);
                Assert.IsType<int>(quantity);
                Assert.IsType<double>(tax);

                var p = (double)price;
                var q = (int)quantity;
                var t = (double)tax;

                var subtotal = p * q;
                var taxAmount = subtotal * t;
                var total = subtotal + taxAmount;

                totals.Add(total);
            }

            // Item1: 100 * 2 = 200, tax = 20, total = 220
            // Item2: 50 * 3 = 150, tax = 15, total = 165
            Assert.Equal(220.0, totals[0], precision: 2);
            Assert.Equal(165.0, totals[1], precision: 2);
        }

        [Fact]
        public async Task TestDataAggregation()
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
                Assert.IsType<double>(current);

                var val = (double)current;
                sum += val;
                count++;
            }

            var average = sum / count;

            Assert.Equal(150.0, sum, precision: 2);
            Assert.Equal(5, count);
            Assert.Equal(30.0, average, precision: 2);
        }

        [Fact]
        public async Task TestComplexConditionals()
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
                    Assert.IsType<bool>(dict["ageCheck"]);
                    Assert.IsType<bool>(dict["premiumCheck"]);
                    Assert.IsType<bool>(dict["countryCheck"]);

                    var ageOk = dict["ageCheck"] is bool a && a;
                    var premiumOk = dict["premiumCheck"] is bool p && p;
                    var countryOk = dict["countryCheck"] is bool c && c;

                    if (ageOk && premiumOk && countryOk)
                    {
                        qualified.Add(user.name);
                    }
                }
            }

            // Only Alice and Diana should qualify
            Assert.Equal(2, qualified.Count);
            Assert.Contains("Alice", qualified);
            Assert.Contains("Diana", qualified);
        }

        [Fact]
        public async Task TestVariableStorageAndRetrieval()
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
            Assert.IsType<int>(userCount);
            Assert.Equal(3, (int)userCount);

            var processedUsers = workflowState.GetByPath("processedUsers")?.CoerceToType(typeof(string[]));
            Assert.IsType<string[]>(processedUsers);
            Assert.Equal(3, ((string[])processedUsers).Length);

            var processedAt = workflowState.GetByPath("metadata.processedAt")?.CoerceToType(typeof(string));
            Assert.IsType<string>(processedAt);
            Assert.Equal("2024-01-01", (string)processedAt);

            var success = workflowState.GetByPath("metadata.success")?.CoerceToType(typeof(bool));
            Assert.IsType<bool>(success);
            Assert.True((bool)success);
        }
    }
}
