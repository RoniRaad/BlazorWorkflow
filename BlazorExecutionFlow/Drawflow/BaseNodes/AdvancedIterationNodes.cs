using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BlazorExecutionFlow.Drawflow.Attributes;
using BlazorExecutionFlow.Models;
using BlazorExecutionFlow.Models.NodeV2;

namespace BlazorExecutionFlow.Drawflow.BaseNodes
{
    /// <summary>
    /// Advanced iteration nodes that can execute downstream nodes multiple times.
    /// These nodes leverage the ClearResult mechanism to re-execute subgraphs.
    /// </summary>
    public static class AdvancedIterationNodes
    {
        /// <summary>
        /// Iterates over a string collection and executes the "item" port for each element.
        /// Each iteration clears and re-executes all downstream nodes from the "item" port.
        /// The current item is available in output.currentItem and output.currentIndex.
        /// After all iterations complete, executes the "done" port with collected results.
        /// </summary>
        [NodeFlowPorts("item", "done")]
        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static async Task<ForEachResult> ForEachString(List<string> collection, NodeContext context)
        {
            var results = new List<JsonNode?>();

            if (collection == null || collection.Count == 0)
            {
                await context.ExecutePortAsync("done");
                return new ForEachResult
                {
                    Results = results,
                    ItemCount = 0
                };
            }

            // OPTIMIZATION: Cache downstream nodes once instead of recursively finding them each iteration
            var downstreamNodes = context.CurrentNode.GetDownstreamNodes("item");

            // Pre-allocate output object to reduce GC pressure
            var outputData = new JsonObject();
            var iterationOutput = new JsonObject { ["output"] = outputData };

            for (int i = 0; i < collection.Count; i++)
            {
                var item = collection[i];

                // Fast clear using cached node list (O(n) instead of O(n * iterations))
                Node.ClearNodes(downstreamNodes);

                // Update the output data (reuse object)
                outputData["currentItem"] = JsonSerializer.SerializeToNode(item);
                outputData["currentIndex"] = i;
                outputData["totalCount"] = collection.Count;
                outputData["isFirst"] = i == 0;
                outputData["isLast"] = i == collection.Count - 1;

                // Temporarily override the current node's result
                var originalResult = context.CurrentNode.Result;
                context.CurrentNode.Result = iterationOutput;

                try
                {
                    // Execute the "item" port with the current item
                    await context.ExecutePortAsync("item");

                    // Collect results from the executed subgraph if needed
                    results.Add(JsonSerializer.SerializeToNode(new
                    {
                        index = i,
                        item = item,
                        processed = true
                    }));
                }
                finally
                {
                    // Restore original result
                    context.CurrentNode.Result = originalResult;
                }
            }

            // Execute the "done" port with all results
            await context.ExecutePortAsync("done");

            return new ForEachResult
            {
                Results = results,
                ItemCount = collection.Count
            };
        }

        /// <summary>
        /// Iterates over a number collection and executes the "item" port for each element.
        /// Each iteration clears and re-executes all downstream nodes from the "item" port.
        /// The current item is available in output.currentItem and output.currentIndex.
        /// After all iterations complete, executes the "done" port with collected results.
        /// </summary>
        [NodeFlowPorts("item", "done")]
        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static async Task<ForEachResult> ForEachNumber(List<int> collection, NodeContext context)
        {
            var results = new List<JsonNode?>();

            if (collection == null || collection.Count == 0)
            {
                await context.ExecutePortAsync("done");
                return new ForEachResult
                {
                    Results = results,
                    ItemCount = 0
                };
            }

            // OPTIMIZATION: Cache downstream nodes once
            var downstreamNodes = context.CurrentNode.GetDownstreamNodes("item");
            var outputData = new JsonObject();
            var iterationOutput = new JsonObject { ["output"] = outputData };

            for (int i = 0; i < collection.Count; i++)
            {
                var item = collection[i];

                // Fast clear using cached node list
                Node.ClearNodes(downstreamNodes);

                // Update the output data (reuse object)
                outputData["currentItem"] = item;
                outputData["currentIndex"] = i;
                outputData["totalCount"] = collection.Count;
                outputData["isFirst"] = i == 0;
                outputData["isLast"] = i == collection.Count - 1;

                var originalResult = context.CurrentNode.Result;
                context.CurrentNode.Result = iterationOutput;

                try
                {
                    await context.ExecutePortAsync("item");

                    results.Add(JsonSerializer.SerializeToNode(new
                    {
                        index = i,
                        item = item,
                        processed = true
                    }));
                }
                finally
                {
                    context.CurrentNode.Result = originalResult;
                }
            }

            await context.ExecutePortAsync("done");

            return new ForEachResult
            {
                Results = results,
                ItemCount = collection.Count
            };
        }

        /// <summary>
        /// Executes the "loop" port repeatedly while the condition port returns true.
        /// Maximum iterations can be specified to prevent infinite loops.
        /// </summary>
        [NodeFlowPorts("loop", "done")]
        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static async Task<WhileResult> WhileLoop(
            bool initialCondition,
            [BlazorFlowInputField] int maxIterations,
            NodeContext context)
        {
            int iterations = 0;
            bool shouldContinue = initialCondition;

            // OPTIMIZATION: Cache downstream nodes once
            var downstreamNodes = context.CurrentNode.GetDownstreamNodes("loop");
            var outputData = new JsonObject();
            var iterationOutput = new JsonObject { ["output"] = outputData };

            while (shouldContinue && iterations < maxIterations)
            {
                // Fast clear using cached node list
                Node.ClearNodes(downstreamNodes);

                // Update iteration info (reuse object)
                outputData["iteration"] = iterations;
                outputData["maxIterations"] = maxIterations;

                var originalResult = context.CurrentNode.Result;
                context.CurrentNode.Result = iterationOutput;

                try
                {
                    await context.ExecutePortAsync("loop");

                    iterations++;
                    // TODO: Add mechanism to check condition from downstream nodes
                }
                finally
                {
                    context.CurrentNode.Result = originalResult;
                }
            }

            await context.ExecutePortAsync("done");

            return new WhileResult
            {
                IterationsExecuted = iterations,
                CompletedNormally = iterations < maxIterations
            };
        }

        /// <summary>
        /// Executes the "body" port a specified number of times.
        /// Each iteration provides the current counter value in output.counter.
        /// </summary>
        [NodeFlowPorts("body", "done")]
        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static async Task<RepeatResult> Repeat(
            int times,
            NodeContext context)
        {
            if (times < 0)
                throw new System.ArgumentException("Times must be non-negative", nameof(times));

            // OPTIMIZATION: Cache downstream nodes once
            var downstreamNodes = context.CurrentNode.GetDownstreamNodes("body");
            var outputData = new JsonObject();
            var iterationOutput = new JsonObject { ["output"] = outputData };

            for (int i = 0; i < times; i++)
            {
                // Fast clear using cached node list
                Node.ClearNodes(downstreamNodes);

                // Update iteration info (reuse object)
                outputData["counter"] = i;
                outputData["total"] = times;
                outputData["isFirst"] = i == 0;
                outputData["isLast"] = i == times - 1;

                var originalResult = context.CurrentNode.Result;
                context.CurrentNode.Result = iterationOutput;

                try
                {
                    await context.ExecutePortAsync("body");
                }
                finally
                {
                    context.CurrentNode.Result = originalResult;
                }
            }

            await context.ExecutePortAsync("done");

            return new RepeatResult
            {
                TimesExecuted = times
            };
        }

        /// <summary>
        /// Maps each string in a collection through the "transform" port.
        /// Executes the transform port for each item and collects the outputs.
        /// The transformed collection is available in the "done" port output.
        /// </summary>
        [NodeFlowPorts("transform", "done")]
        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static async Task<MapResult> MapStrings(
            List<string> collection,
            NodeContext context)
        {
            var results = new List<JsonNode?>();

            if (collection == null || collection.Count == 0)
            {
                await context.ExecutePortAsync("done");
                return new MapResult { TransformedItems = results };
            }

            // OPTIMIZATION: Cache downstream nodes once
            var downstreamNodes = context.CurrentNode.GetDownstreamNodes("transform");
            var outputData = new JsonObject();
            var iterationOutput = new JsonObject { ["output"] = outputData };

            for (int i = 0; i < collection.Count; i++)
            {
                var item = collection[i];

                // Fast clear using cached node list
                Node.ClearNodes(downstreamNodes);

                // Update output data (reuse object)
                outputData["item"] = item;
                outputData["index"] = i;

                var originalResult = context.CurrentNode.Result;
                context.CurrentNode.Result = iterationOutput;

                try
                {
                    await context.ExecutePortAsync("transform");

                    results.Add(JsonSerializer.SerializeToNode(item));
                }
                finally
                {
                    context.CurrentNode.Result = originalResult;
                }
            }

            await context.ExecutePortAsync("done");

            return new MapResult { TransformedItems = results };
        }

        /// <summary>
        /// Maps each number in a collection through the "transform" port.
        /// Executes the transform port for each item and collects the outputs.
        /// The transformed collection is available in the "done" port output.
        /// </summary>
        [NodeFlowPorts("transform", "done")]
        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static async Task<MapResult> MapNumbers(
            List<int> collection,
            NodeContext context)
        {
            var results = new List<JsonNode?>();

            if (collection == null || collection.Count == 0)
            {
                await context.ExecutePortAsync("done");
                return new MapResult { TransformedItems = results };
            }

            // OPTIMIZATION: Cache downstream nodes once
            var downstreamNodes = context.CurrentNode.GetDownstreamNodes("transform");
            var outputData = new JsonObject();
            var iterationOutput = new JsonObject { ["output"] = outputData };

            for (int i = 0; i < collection.Count; i++)
            {
                var item = collection[i];

                // Fast clear using cached node list
                Node.ClearNodes(downstreamNodes);

                // Update output data (reuse object)
                outputData["item"] = JsonSerializer.SerializeToNode(item);
                outputData["index"] = i;

                var originalResult = context.CurrentNode.Result;
                context.CurrentNode.Result = iterationOutput;

                try
                {
                    await context.ExecutePortAsync("transform");

                    results.Add(JsonSerializer.SerializeToNode(item));
                }
                finally
                {
                    context.CurrentNode.Result = originalResult;
                }
            }

            await context.ExecutePortAsync("done");

            return new MapResult { TransformedItems = results };
        }

        /// <summary>
        /// Iterates over a JSON collection (mixed types, objects, etc.) and executes the "item" port for each element.
        /// Each iteration clears and re-executes all downstream nodes from the "item" port.
        /// The current item is available in output.currentItem and output.currentIndex.
        /// After all iterations complete, executes the "done" port with collected results.
        /// Use this for arrays of objects, mixed-type arrays, or any complex JSON data.
        /// </summary>
        [NodeFlowPorts("item", "done")]
        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static async Task<ForEachResult> ForEachJson(List<JsonNode> collection, NodeContext context)
        {
            var results = new List<JsonNode?>();

            if (collection == null || collection.Count == 0)
            {
                await context.ExecutePortAsync("done");
                return new ForEachResult
                {
                    Results = results,
                    ItemCount = 0
                };
            }

            // OPTIMIZATION: Cache downstream nodes once
            var downstreamNodes = context.CurrentNode.GetDownstreamNodes("item");
            var outputData = new JsonObject();
            var iterationOutput = new JsonObject { ["output"] = outputData };

            for (int i = 0; i < collection.Count; i++)
            {
                var item = collection[i];

                // Fast clear using cached node list
                Node.ClearNodes(downstreamNodes);

                // Update output data (reuse object)
                outputData["currentItem"] = item;
                outputData["currentIndex"] = i;
                outputData["totalCount"] = collection.Count;
                outputData["isFirst"] = i == 0;
                outputData["isLast"] = i == collection.Count - 1;

                var originalResult = context.CurrentNode.Result;
                context.CurrentNode.Result = iterationOutput;

                try
                {
                    await context.ExecutePortAsync("item");

                    results.Add(JsonSerializer.SerializeToNode(new
                    {
                        index = i,
                        item = item,
                        processed = true
                    }));
                }
                finally
                {
                    context.CurrentNode.Result = originalResult;
                }
            }

            await context.ExecutePortAsync("done");

            return new ForEachResult
            {
                Results = results,
                ItemCount = collection.Count
            };
        }

        /// <summary>
        /// Maps each JSON item through the "transform" port.
        /// Executes the transform port for each item and collects the outputs.
        /// The transformed collection is available in the "done" port output.
        /// </summary>
        [NodeFlowPorts("transform", "done")]
        [BlazorFlowNodeMethod(NodeType.Function, "Collections")]
        public static async Task<MapResult> MapJson(
            List<JsonNode> collection,
            NodeContext context)
        {
            var results = new List<JsonNode?>();

            if (collection == null || collection.Count == 0)
            {
                await context.ExecutePortAsync("done");
                return new MapResult { TransformedItems = results };
            }

            // OPTIMIZATION: Cache downstream nodes once
            var downstreamNodes = context.CurrentNode.GetDownstreamNodes("transform");
            var outputData = new JsonObject();
            var iterationOutput = new JsonObject { ["output"] = outputData };

            for (int i = 0; i < collection.Count; i++)
            {
                var item = collection[i];

                // Fast clear using cached node list
                Node.ClearNodes(downstreamNodes);

                // Update output data (reuse object)
                outputData["item"] = item;
                outputData["index"] = i;

                var originalResult = context.CurrentNode.Result;
                context.CurrentNode.Result = iterationOutput;

                try
                {
                    await context.ExecutePortAsync("transform");

                    results.Add(item);
                }
                finally
                {
                    context.CurrentNode.Result = originalResult;
                }
            }

            await context.ExecutePortAsync("done");

            return new MapResult { TransformedItems = results };
        }
    }

    // Result types for iteration nodes
    public class ForEachResult
    {
        public List<JsonNode?> Results { get; set; } = new();
        public int ItemCount { get; set; }
    }

    public class WhileResult
    {
        public int IterationsExecuted { get; set; }
        public bool CompletedNormally { get; set; }
    }

    public class RepeatResult
    {
        public int TimesExecuted { get; set; }
    }

    public class MapResult
    {
        public List<JsonNode?> TransformedItems { get; set; } = new();
    }
}
