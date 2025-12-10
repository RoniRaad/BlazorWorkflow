using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Helpers;
namespace BlazorExecutionFlow.Models.NodeV2
{
    public class Graph
    {
        public ConcurrentDictionary<string, Node> Nodes = [];
        private readonly static MethodInfo startMethod = typeof(CoreNodes).GetMethod(nameof(CoreNodes.Start))!;

        public Graph()
        {
            Nodes["0"] = DrawflowHelpers.CreateNodeFromMethod(startMethod);
            Nodes["0"].DrawflowNodeId = "0";
        }

        public async Task Run(GraphExecutionContext executionContext)
        {
            // Clear results and errors from previous runs
            foreach (var node in Nodes)
            {
                node.Value.Result = null;
                node.Value.HasError = false;
                node.Value.ErrorMessage = null;
                node.Value.SharedExecutionContext = executionContext;
            }

            // Event handlers are already attached during component initialization
            // No need to re-attach them here

            var startNodes = Nodes.Where(x => x.Value.BackingMethod == startMethod);
            var tasks = new List<Task>();
            executionContext.StartTime = DateTime.Now;

            foreach (var startNode in startNodes)
            {
                tasks.Add(startNode.Value.ExecuteNode());
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    public class GraphExecutionContext
    {
        private JsonObject _sharedContext = new JsonObject();
        public DateTimeOffset StartTime { get; set; } = DateTimeOffset.Now;
        public FrozenDictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>().ToFrozenDictionary();
        public FrozenDictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>().ToFrozenDictionary();
        public JsonObject SharedContext {
            get
            {
                // Add workflow object with parameters
                var workflowObj = new JsonObject();
                _sharedContext.TryAdd("workflow", workflowObj);
                workflowObj.TryAdd("parameters", JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(Parameters))!);

                // Add environment variables at root level
                _sharedContext.TryAdd("environment", JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(EnvironmentVariables))!);

                return _sharedContext;
            }
        }
    }
}