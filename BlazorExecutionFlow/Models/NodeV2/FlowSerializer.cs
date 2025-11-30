using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorExecutionFlow.Models.NodeV2
{
    /// <summary>
    /// Serializes and deserializes node graphs to/from JSON strings.
    /// Preserves all node configurations, mappings, connections, and execution state.
    /// </summary>
    public static class FlowSerializer
    {
        private static readonly JsonSerializerOptions DefaultOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Serializes a collection of nodes to a JSON string.
        /// </summary>
        public static string SerializeFlow(IEnumerable<Node> nodes, string? flowName = null, Dictionary<string, object>? metadata = null)
        {
            var nodeList = nodes.ToList();
            var serializableNodes = new List<SerializableNode>();
            var nodeIdMap = new Dictionary<Node, string>();

            // First pass: create IDs for all nodes
            for (int i = 0; i < nodeList.Count; i++)
            {
                nodeIdMap[nodeList[i]] = nodeList[i].Id;
            }

            // Second pass: serialize nodes with connection references
            foreach (var node in nodeList)
            {
                var serializableNode = new SerializableNode
                {
                    Id = nodeIdMap[node],
                    Section = node.Section,
                    DrawflowNodeId = node.DrawflowNodeId,
                    PosX = node.PosX,
                    PosY = node.PosY,
                    MethodSignature = MethodInfoHelpers.ToSerializableString(node.BackingMethod),
                    NodeInputToMethodInputMap = node.NodeInputToMethodInputMap,
                    MethodOutputToNodeOutputMap = node.MethodOutputToNodeOutputMap,
                    DictionaryParameterMappings = node.DictionaryParameterMappings,
                    Input = node.Input,
                    Result = node.Result,
                    MergeOutputWithInput = node.MergeOutputWithInput,
                    DeclaredOutputPorts = node.DeclaredOutputPorts,
                    InputNodeIds = node.InputNodes.Select(n => nodeIdMap[n]).ToList(),
                    OutputNodeIds = node.OutputNodes.Select(n => nodeIdMap[n]).ToList(),
                    OutputPortConnections = node.OutputPorts.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Select(n => nodeIdMap[n]).ToList()
                    ),
                    ParentWorkflowId = node.ParentWorkflowId,
                    NameOverride = node.NameOverride
                };

                serializableNodes.Add(serializableNode);
            }

            var flow = new SerializableFlow
            {
                Version = "1.0",
                FlowName = flowName ?? "Untitled Flow",
                CreatedAt = DateTime.UtcNow,
                Metadata = metadata ?? new Dictionary<string, object>(),
                Nodes = serializableNodes
            };

            return JsonSerializer.Serialize(flow, DefaultOptions);
        }

        /// <summary>
        /// Deserializes a JSON string to a collection of nodes with all connections restored.
        /// </summary>
        public static List<Node> DeserializeFlow(string json, out FlowMetadata metadata)
        {
            var flow = JsonSerializer.Deserialize<SerializableFlow>(json, DefaultOptions)
                ?? throw new InvalidOperationException("Failed to deserialize flow JSON.");

            metadata = new FlowMetadata
            {
                Version = flow.Version,
                FlowName = flow.FlowName,
                CreatedAt = flow.CreatedAt,
                Metadata = flow.Metadata
            };

            var nodes = new List<Node>();
            var nodeMap = new Dictionary<string, Node>();

            // Remove duplicate nodes (keep first occurrence of each unique ID)
            var uniqueNodes = new Dictionary<string, SerializableNode>();
            foreach (var serNode in flow.Nodes)
            {
                if (!uniqueNodes.ContainsKey(serNode.Id))
                {
                    uniqueNodes[serNode.Id] = serNode;
                }
            }

            // First pass: create all node objects
            foreach (var serNode in uniqueNodes.Values)
            {
                var method = MethodInfoHelpers.FromSerializableString(serNode.MethodSignature);

                var node = new Node
                {
                    BackingMethod = method,
                    Id = serNode.Id,
                    Section = serNode.Section ?? string.Empty,
                    DrawflowNodeId = serNode.DrawflowNodeId ?? string.Empty,
                    PosX = serNode.PosX,
                    PosY = serNode.PosY,
                    NodeInputToMethodInputMap = serNode.NodeInputToMethodInputMap ?? new List<PathMapEntry>(),
                    MethodOutputToNodeOutputMap = serNode.MethodOutputToNodeOutputMap ?? new List<PathMapEntry>(),
                    DictionaryParameterMappings = serNode.DictionaryParameterMappings ?? new Dictionary<string, List<PathMapEntry>>(),
                    Input = serNode.Input,
                    Result = serNode.Result,
                    MergeOutputWithInput = serNode.MergeOutputWithInput,
                    DeclaredOutputPorts = serNode.DeclaredOutputPorts ?? new List<string>(),
                    ParentWorkflowId = serNode.ParentWorkflowId,
                    NameOverride = serNode.NameOverride
                };

                if (node.IsWorkflowNode)
                {
                    var workflowService = Helpers.NodeServiceProvider.Instance.GetService<IWorkflowService>();
                    var workflow = workflowService.GetWorkflow(node.ParentWorkflowId);
                    var discoveredInputs = WorkflowInputDiscovery.DiscoverInputs(workflow.FlowGraph);
                    var newInputMap = new List<PathMapEntry>();
                    foreach (var input in discoveredInputs)
                    {
                        var currentMap = node.NodeInputToMethodInputMap.FirstOrDefault(x => x.To == input);
                        if (currentMap == null)
                        {
                            newInputMap.Add(new PathMapEntry() { To = input });
                        }
                        else
                        {
                            newInputMap.Add(currentMap);
                        }
                    }

                    // We replace it so that if the inputs on the workflow are changed stale input maps are removed.
                    node.NodeInputToMethodInputMap = newInputMap;
                }

                nodes.Add(node);
                nodeMap[serNode.Id] = node;
            }

            // Second pass: restore connections
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var serNode = uniqueNodes[node.Id];

                // Restore input connections
                foreach (var inputId in serNode.InputNodeIds)
                {
                    if (nodeMap.TryGetValue(inputId, out var inputNode))
                    {
                        node.InputNodes.Add(inputNode);
                    }
                }

                // Restore output connections
                foreach (var outputId in serNode.OutputNodeIds)
                {
                    if (nodeMap.TryGetValue(outputId, out var outputNode))
                    {
                        node.OutputNodes.Add(outputNode);
                    }
                }

                // Restore output port connections
                foreach (var (portName, connectedIds) in serNode.OutputPortConnections)
                {
                    foreach (var connectedId in connectedIds)
                    {
                        if (nodeMap.TryGetValue(connectedId, out var connectedNode))
                        {
                            node.AddOutputConnection(portName, connectedNode);
                        }
                    }
                }
            }

            return nodes;
        }

        /// <summary>
        /// Simplified deserialization that only returns nodes without metadata.
        /// </summary>
        public static List<Node> DeserializeFlow(string json)
        {
            return DeserializeFlow(json, out _);
        }

        /// <summary>
        /// Validates a flow JSON string without fully deserializing it.
        /// </summary>
        public static bool ValidateFlow(string json, out string? errorMessage)
        {
            try
            {
                var flow = JsonSerializer.Deserialize<SerializableFlow>(json, DefaultOptions);
                if (flow == null)
                {
                    errorMessage = "Flow JSON is null after deserialization.";
                    return false;
                }

                if (flow.Nodes == null || flow.Nodes.Count == 0)
                {
                    errorMessage = "Flow contains no nodes.";
                    return false;
                }

                // Check for duplicate node IDs
                var ids = new HashSet<string>();
                foreach (var node in flow.Nodes)
                {
                    if (!ids.Add(node.Id))
                    {
                        errorMessage = $"Duplicate node ID found: {node.Id}";
                        return false;
                    }
                }

                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Validation failed: {ex.Message}";
                return false;
            }
        }
    }

    /// <summary>
    /// Serializable representation of a flow.
    /// </summary>
    internal class SerializableFlow
    {
        public string Version { get; set; } = "1.0";
        public string FlowName { get; set; } = "Untitled Flow";
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<SerializableNode> Nodes { get; set; } = new();
    }

    /// <summary>
    /// Serializable representation of a node.
    /// </summary>
    internal class SerializableNode
    {
        public string Id { get; set; } = string.Empty;
        public string? Section { get; set; }
        public string? DrawflowNodeId { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
        public string MethodSignature { get; set; } = string.Empty;
        public List<PathMapEntry> NodeInputToMethodInputMap { get; set; } = new();
        public List<PathMapEntry> MethodOutputToNodeOutputMap { get; set; } = new();
        public Dictionary<string, List<PathMapEntry>> DictionaryParameterMappings { get; set; } = new();
        public JsonObject? Input { get; set; }
        public JsonObject? Result { get; set; }
        public bool MergeOutputWithInput { get; set; }
        public List<string> DeclaredOutputPorts { get; set; } = new();
        public List<string> InputNodeIds { get; set; } = new();
        public List<string> OutputNodeIds { get; set; } = new();
        public Dictionary<string, List<string>> OutputPortConnections { get; set; } = new();
        public string? ParentWorkflowId { get; set; }
        public string? NameOverride { get; set; }
    }

    /// <summary>
    /// Metadata about a deserialized flow.
    /// </summary>
    public class FlowMetadata
    {
        public string Version { get; set; } = "1.0";
        public string FlowName { get; set; } = "Untitled Flow";
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
