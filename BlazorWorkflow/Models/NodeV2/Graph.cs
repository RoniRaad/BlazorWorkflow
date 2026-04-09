using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using BlazorWorkflow.Flow.BaseNodes;
using BlazorWorkflow.Helpers;
namespace BlazorWorkflow.Models.NodeV2
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

        public Task Run(GraphExecutionContext executionContext)
        {
            return Task.Run(async () => 
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
            });
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Level { get; set; } = "info";
        public string Message { get; set; } = string.Empty;
        public string? NodeName { get; set; }
    }

    public class GraphExecutionContext
    {
        private JsonObject _sharedContext = new JsonObject();
        private readonly List<LogEntry> _logs = new();
        private readonly object _logLock = new();

        public DateTimeOffset StartTime { get; set; } = DateTimeOffset.Now;
        public FrozenDictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>().ToFrozenDictionary();
        public FrozenDictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>().ToFrozenDictionary();

        public IReadOnlyList<LogEntry> Logs
        {
            get { lock (_logLock) { return _logs.ToList().AsReadOnly(); } }
        }

        public void AddLog(string message, string level = "info", string? nodeName = null)
        {
            lock (_logLock)
            {
                _logs.Add(new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = level,
                    Message = message,
                    NodeName = nodeName
                });
            }
        }
        private bool _contextInitialized;

        public JsonObject SharedContext {
            get
            {
                if (!_contextInitialized)
                {
                    // Add workflow object with parameters
                    var workflowObj = new JsonObject();
                    workflowObj["parameters"] = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(Parameters))!;
                    _sharedContext["workflow"] = workflowObj;

                    // Add environment variables at root level
                    _sharedContext["environment"] = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(EnvironmentVariables))!;

                    _contextInitialized = true;
                }

                return _sharedContext;
            }
        }
    }
}