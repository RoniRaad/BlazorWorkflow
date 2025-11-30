using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Flow.Attributes;
using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Models.NodeV2;

namespace TestRunner
{
    public static class NodeTests
    {

        public static async Task RunAllTests()
        {
            Console.WriteLine("Running Iteration Node Performance Tests\n");

            // Test the core optimization: GetDownstreamNodes caching
            TestGetDownstreamNodesCaching();

            Console.WriteLine();
            Console.WriteLine(new string('-', 80));
            Console.WriteLine("Running Integration Performance Benchmarks\n");

            // Integration benchmarks
            await BenchmarkForEachString();
            await BenchmarkForEachNumber();
            await BenchmarkForEachJson();
            await BenchmarkRepeat();
            await BenchmarkMapStrings();
            await BenchmarkMapNumbers();
            await BenchmarkMapJson();

            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("Performance Summary:");
            Console.WriteLine("  The optimizations reduce graph traversal complexity from O(n × iterations)");
            Console.WriteLine("  to O(n + iterations) by caching downstream nodes once before the loop.");
            Console.WriteLine("  Object reuse further reduces GC pressure during iteration.");
            Console.WriteLine(new string('=', 80));
        }

        // Test the core optimization mechanism
        private static void TestGetDownstreamNodesCaching()
        {
            Console.WriteLine("[TEST] GetDownstreamNodes Caching Performance");
            Console.WriteLine("  Testing downstream node lookup caching optimization\n");

            var sizes = new[] { 10, 50, 100, 500 };

            foreach (var downstreamNodeCount in sizes)
            {
                // Create a test node graph
                var rootNode = CreateTestNode("root");
                var downstreamNodes = CreateDownstreamChain(rootNode, downstreamNodeCount, "item");

                // Simulate OLD approach: find downstream nodes on EACH iteration
                var sw1 = Stopwatch.StartNew();
                for (int i = 0; i < 100; i++)
                {
                    var nodes = rootNode.GetDownstreamNodes("item"); // Repeated BFS traversal
                    Node.ClearNodes(nodes);
                }
                sw1.Stop();
                var oldApproachTime = sw1.ElapsedMilliseconds;

                // Simulate NEW approach: cache downstream nodes ONCE
                var sw2 = Stopwatch.StartNew();
                var cachedNodes = rootNode.GetDownstreamNodes("item"); // Single BFS traversal
                for (int i = 0; i < 100; i++)
                {
                    Node.ClearNodes(cachedNodes); // Fast clear with cached list
                }
                sw2.Stop();
                var newApproachTime = sw2.ElapsedMilliseconds;

                var speedup = oldApproachTime > 0 ? (double)oldApproachTime / newApproachTime : 1.0;

                Console.WriteLine($"  Downstream nodes: {downstreamNodeCount,3} | " +
                                  $"Old: {oldApproachTime,4}ms | " +
                                  $"New: {newApproachTime,4}ms | " +
                                  $"Speedup: {speedup:F2}x");
            }

            Console.WriteLine();
        }

        // Integration Benchmarks
        private static async Task BenchmarkForEachString()
        {
            Console.WriteLine("[BENCHMARK] ForEachString Performance");

            var sizes = new[] { 10, 50, 100, 500, 1000 };

            foreach (var size in sizes)
            {
                var collection = Enumerable.Range(0, size).Select(i => $"item_{i}").ToList();
                var nodeContext = CreateMockContext();

                var sw = Stopwatch.StartNew();
                var result = await AdvancedIterationNodes.ForEachString(collection, nodeContext);
                sw.Stop();

                var itemsPerMs = size / Math.Max(sw.ElapsedMilliseconds, 1.0);
                Console.WriteLine($"  Collection size: {size,4} | Time: {sw.ElapsedMilliseconds,6}ms | " +
                                  $"Throughput: {itemsPerMs:F2} items/ms");

                Assert(result.ItemCount == size, $"ForEachString should process {size} items");
            }

            Console.WriteLine();
        }

        private static async Task BenchmarkForEachNumber()
        {
            Console.WriteLine("[BENCHMARK] ForEachNumber Performance");

            var sizes = new[] { 10, 50, 100, 500, 1000 };

            foreach (var size in sizes)
            {
                var collection = Enumerable.Range(0, size).ToList();
                var nodeContext = CreateMockContext();

                var sw = Stopwatch.StartNew();
                var result = await AdvancedIterationNodes.ForEachNumber(collection, nodeContext);
                sw.Stop();

                var itemsPerMs = size / Math.Max(sw.ElapsedMilliseconds, 1.0);
                Console.WriteLine($"  Collection size: {size,4} | Time: {sw.ElapsedMilliseconds,6}ms | " +
                                  $"Throughput: {itemsPerMs:F2} items/ms");

                Assert(result.ItemCount == size, $"ForEachNumber should process {size} items");
            }

            Console.WriteLine();
        }

        private static async Task BenchmarkForEachJson()
        {
            Console.WriteLine("[BENCHMARK] ForEachJson Performance");

            var sizes = new[] { 10, 50, 100, 500, 1000 };

            foreach (var size in sizes)
            {
                var collection = Enumerable.Range(0, size)
                    .Select(i => JsonNode.Parse($"{{\"id\": {i}, \"value\": \"item_{i}\"}}")!)
                    .ToList();
                var nodeContext = CreateMockContext();

                var sw = Stopwatch.StartNew();
                var result = await AdvancedIterationNodes.ForEachJson(collection, nodeContext);
                sw.Stop();

                var itemsPerMs = size / Math.Max(sw.ElapsedMilliseconds, 1.0);
                Console.WriteLine($"  Collection size: {size,4} | Time: {sw.ElapsedMilliseconds,6}ms | " +
                                  $"Throughput: {itemsPerMs:F2} items/ms");

                Assert(result.ItemCount == size, $"ForEachJson should process {size} items");
            }

            Console.WriteLine();
        }

        private static async Task BenchmarkRepeat()
        {
            Console.WriteLine("[BENCHMARK] Repeat Performance - SKIPPED (Method not available)");
            Console.WriteLine();
            await Task.CompletedTask;

            // Note: AdvancedIterationNodes.Repeat method does not exist
        }

        private static async Task BenchmarkMapStrings()
        {
            Console.WriteLine("[BENCHMARK] MapStrings Performance");

            var sizes = new[] { 10, 50, 100, 500, 1000 };

            foreach (var size in sizes)
            {
                var collection = Enumerable.Range(0, size).Select(i => $"item_{i}").ToList();
                var nodeContext = CreateMockContext();

                var sw = Stopwatch.StartNew();
                var result = await AdvancedIterationNodes.MapStrings(collection, nodeContext);
                sw.Stop();

                var itemsPerMs = size / Math.Max(sw.ElapsedMilliseconds, 1.0);
                Console.WriteLine($"  Collection size: {size,4} | Time: {sw.ElapsedMilliseconds,6}ms | " +
                                  $"Throughput: {itemsPerMs:F2} items/ms");

                Assert(result.TransformedItems.Count == size, $"MapStrings should return {size} items");
            }

            Console.WriteLine();
        }

        private static async Task BenchmarkMapNumbers()
        {
            Console.WriteLine("[BENCHMARK] MapNumbers Performance");

            var sizes = new[] { 10, 50, 100, 500, 1000 };

            foreach (var size in sizes)
            {
                var collection = Enumerable.Range(0, size).ToList();
                var nodeContext = CreateMockContext();

                var sw = Stopwatch.StartNew();
                var result = await AdvancedIterationNodes.MapNumbers(collection, nodeContext);
                sw.Stop();

                var itemsPerMs = size / Math.Max(sw.ElapsedMilliseconds, 1.0);
                Console.WriteLine($"  Collection size: {size,4} | Time: {sw.ElapsedMilliseconds,6}ms | " +
                                  $"Throughput: {itemsPerMs:F2} items/ms");

                Assert(result.TransformedItems.Count == size, $"MapNumbers should return {size} items");
            }

            Console.WriteLine();
        }

        private static async Task BenchmarkMapJson()
        {
            Console.WriteLine("[BENCHMARK] MapJson Performance");

            var sizes = new[] { 10, 50, 100, 500, 1000 };

            foreach (var size in sizes)
            {
                var collection = Enumerable.Range(0, size)
                    .Select(i => JsonNode.Parse($"{{\"value\": {i}}}")!)
                    .ToList();
                var nodeContext = CreateMockContext();

                var sw = Stopwatch.StartNew();
                var result = await AdvancedIterationNodes.MapJson(collection, nodeContext);
                sw.Stop();

                var itemsPerMs = size / Math.Max(sw.ElapsedMilliseconds, 1.0);
                Console.WriteLine($"  Collection size: {size,4} | Time: {sw.ElapsedMilliseconds,6}ms | " +
                                  $"Throughput: {itemsPerMs:F2} items/ms");

                Assert(result.TransformedItems.Count == size, $"MapJson should return {size} items");
            }

            Console.WriteLine();
        }

        // Helper methods
        private static NodeContext CreateMockContext()
        {
            // Get a dummy method for the BackingMethod requirement
            var dummyMethod = typeof(BaseNodeCollection).GetMethod("Add", BindingFlags.Public | BindingFlags.Static)!;

            // Create the iteration node
            var iterationNode = new Node
            {
                BackingMethod = dummyMethod,
                Id = "1",
                Section = "test",
                DrawflowNodeId = "test-iteration-node"
            };

            // Create a realistic downstream subgraph (chain of 10 nodes)
            var downstreamChain = CreateDownstreamChain(iterationNode, 10, "item");

            // Also initialize done port
            iterationNode.OutputPorts["done"] = new List<Node>();

            var context = new NodeContext
            {
                CurrentNode = iterationNode,
                InputNodes = Array.Empty<Node>(),
                OutputNodes = downstreamChain,
                Context = new Dictionary<string, object?>()
            };

            return context;
        }

        private static Node CreateTestNode(string id)
        {
            var dummyMethod = typeof(BaseNodeCollection).GetMethod("Add", BindingFlags.Public | BindingFlags.Static)!;
            return new Node
            {
                BackingMethod = dummyMethod,
                Id = id,
                Section = "test",
                DrawflowNodeId = $"test-{id}"
            };
        }

        private static List<Node> CreateDownstreamChain(Node rootNode, int count, string portName)
        {
            var nodes = new List<Node>();

            if (count == 0)
            {
                rootNode.OutputPorts[portName] = nodes;
                return nodes;
            }

            // Create a chain of nodes
            Node? previousNode = null;

            for (int i = 0; i < count; i++)
            {
                var node = CreateTestNode($"downstream-{i}");
                nodes.Add(node);

                if (i == 0)
                {
                    // First node connects to root's port
                    if (!rootNode.OutputPorts.ContainsKey(portName))
                    {
                        rootNode.OutputPorts[portName] = new List<Node>();
                    }
                    rootNode.OutputPorts[portName].Add(node);
                }
                else
                {
                    // Subsequent nodes form a chain
                    previousNode?.OutputNodes.Add(node);
                }

                previousNode = node;
            }

            return nodes;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }
    }
}
