using System.Reflection;
using System.Text.Json.Nodes;
using BlazorFlow.Helpers;
using BlazorFlow.Models.NodeV2;

namespace BlazorFlow.Testing
{
    /// <summary>
    /// Fluent API for building and testing node graphs programmatically.
    /// </summary>
    public class NodeGraphBuilder
    {
        private readonly List<Node> _nodes = [];
        private readonly Dictionary<string, Node> _nodesByName = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Creates a new node from a method and adds it to the graph.
        /// </summary>
        /// <param name="nodeName">Unique name for this node (used for connections)</param>
        /// <param name="method">The method that backs this node</param>
        /// <returns>A NodeBuilder for configuring this specific node</returns>
        public NodeBuilder AddNode(string nodeName, MethodInfo method)
        {
            if (_nodesByName.ContainsKey(nodeName))
                throw new ArgumentException($"A node with name '{nodeName}' already exists in the graph.", nameof(nodeName));

            var node = new Node
            {
                BackingMethod = method,
                Id = Guid.NewGuid().ToString(),
                DrawflowNodeId = _nodes.Count.ToString()
            };

            _nodes.Add(node);
            _nodesByName[nodeName] = node;

            return new NodeBuilder(this, node, nodeName);
        }

        /// <summary>
        /// Creates a new node from a static method using type and method name.
        /// </summary>
        public NodeBuilder AddNode(string nodeName, Type type, string methodName)
        {
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                throw new ArgumentException($"Static method '{methodName}' not found on type '{type.Name}'.", nameof(methodName));

            return AddNode(nodeName, method);
        }

        /// <summary>
        /// Gets a node by its name.
        /// </summary>
        public Node GetNode(string nodeName)
        {
            if (!_nodesByName.TryGetValue(nodeName, out var node))
                throw new ArgumentException($"Node '{nodeName}' not found in the graph.", nameof(nodeName));

            return node;
        }

        /// <summary>
        /// Connects the output of one node to the input of another.
        /// </summary>
        /// <param name="fromNode">Source node name</param>
        /// <param name="toNode">Target node name</param>
        /// <param name="outputPort">Optional output port name (for multi-port nodes)</param>
        public NodeGraphBuilder Connect(string fromNode, string toNode, string? outputPort = null)
        {
            var source = GetNode(fromNode);
            var target = GetNode(toNode);

            source.AddOutputConnection(outputPort, target);

            if (!target.InputNodes.Contains(source))
                target.InputNodes.Add(source);

            return this;
        }

        /// <summary>
        /// Executes the graph starting from the specified node.
        /// </summary>
        public async Task<GraphExecutionResult> ExecuteAsync(string startNodeName)
        {
            var startNode = GetNode(startNodeName);

            // Clear previous results
            foreach (var node in _nodes)
                node.Result = null;

            await startNode.ExecuteNode();

            return new GraphExecutionResult(_nodesByName);
        }

        /// <summary>
        /// Gets all nodes in the graph.
        /// </summary>
        public IReadOnlyList<Node> GetAllNodes() => _nodes.AsReadOnly();
    }

    /// <summary>
    /// Builder for configuring a specific node.
    /// </summary>
    public class NodeBuilder
    {
        private readonly NodeGraphBuilder _graphBuilder;
        private readonly Node _node;
        private readonly string _nodeName;

        internal NodeBuilder(NodeGraphBuilder graphBuilder, Node node, string nodeName)
        {
            _graphBuilder = graphBuilder;
            _node = node;
            _nodeName = nodeName;
        }

        /// <summary>
        /// Maps an input value to a method parameter.
        /// </summary>
        /// <param name="parameterName">The method parameter name</param>
        /// <param name="inputPath">The input path (e.g., "input.value" or a literal value)</param>
        public NodeBuilder MapInput(string parameterName, string inputPath)
        {
            _node.NodeInputToMethodInputMap.Add(new PathMapEntry
            {
                From = inputPath,
                To = parameterName
            });
            return this;
        }

        /// <summary>
        /// Maps a method output property to a node output.
        /// </summary>
        /// <param name="propertyName">The property name from the method's return value</param>
        /// <param name="outputName">The name for the output (defaults to property name)</param>
        public NodeBuilder MapOutput(string propertyName, string? outputName = null)
        {
            _node.MethodOutputToNodeOutputMap.Add(new PathMapEntry
            {
                From = propertyName,
                To = outputName ?? propertyName
            });
            return this;
        }

        /// <summary>
        /// Automatically maps all return properties as outputs.
        /// Useful for simple nodes where all properties should be exposed.
        /// </summary>
        public NodeBuilder AutoMapOutputs()
        {
            var returnProps = TypeHelpers.GetReturnProperties(_node.BackingMethod);
            if (returnProps != null)
            {
                foreach (var prop in returnProps)
                {
                    if (prop.Name != null)
                    {
                        _node.MethodOutputToNodeOutputMap.Add(new PathMapEntry
                        {
                            From = prop.Name,
                            To = prop.Name
                        });
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Sets the output ports for port-driven flow control nodes.
        /// </summary>
        public NodeBuilder WithOutputPorts(params string[] portNames)
        {
            _node.DeclaredOutputPorts.Clear();
            _node.DeclaredOutputPorts.AddRange(portNames);
            return this;
        }

        /// <summary>
        /// Sets whether to merge output with input payload.
        /// </summary>
        public NodeBuilder MergeOutputWithInput(bool merge = true)
        {
            _node.MergeOutputWithInput = merge;
            return this;
        }

        /// <summary>
        /// Finishes configuring this node and returns to the graph builder.
        /// </summary>
        public NodeGraphBuilder Build() => _graphBuilder;

        /// <summary>
        /// Finishes this node and adds another node to the graph.
        /// </summary>
        public NodeBuilder AddNode(string nodeName, MethodInfo method)
        {
            return _graphBuilder.AddNode(nodeName, method);
        }

        /// <summary>
        /// Finishes this node and adds another node to the graph.
        /// </summary>
        public NodeBuilder AddNode(string nodeName, Type type, string methodName)
        {
            return _graphBuilder.AddNode(nodeName, type, methodName);
        }

        /// <summary>
        /// Connects this node to another node.
        /// </summary>
        public NodeBuilder ConnectTo(string targetNodeName, string? outputPort = null)
        {
            _graphBuilder.Connect(_nodeName, targetNodeName, outputPort);
            return this;
        }
    }

    /// <summary>
    /// Result of a graph execution, providing easy access to node outputs.
    /// </summary>
    public class GraphExecutionResult
    {
        private readonly Dictionary<string, Node> _nodes;

        internal GraphExecutionResult(Dictionary<string, Node> nodes)
        {
            _nodes = nodes;
        }

        /// <summary>
        /// Gets the result of a specific node.
        /// </summary>
        public JsonObject? GetNodeResult(string nodeName)
        {
            if (!_nodes.TryGetValue(nodeName, out var node))
                throw new ArgumentException($"Node '{nodeName}' not found.", nameof(nodeName));

            return node.Result;
        }

        /// <summary>
        /// Gets a specific output value from a node.
        /// </summary>
        public JsonNode? GetOutput(string nodeName, string outputPath)
        {
            var result = GetNodeResult(nodeName);
            return result?.GetByPath($"output.{outputPath}");
        }

        /// <summary>
        /// Gets a typed output value from a node.
        /// </summary>
        public T? GetOutput<T>(string nodeName, string outputPath)
        {
            var value = GetOutput(nodeName, outputPath);
            return value.CoerceToType<T>();
        }

        /// <summary>
        /// Gets the entire output object from a node.
        /// </summary>
        public JsonNode? GetOutputObject(string nodeName)
        {
            var result = GetNodeResult(nodeName);
            return result?.GetByPath("output");
        }
    }

    /// <summary>
    /// Extension methods for JsonNode to simplify type conversion.
    /// </summary>
    public static class JsonNodeTestExtensions
    {
        public static T? CoerceToType<T>(this JsonNode? node)
        {
            return (T?)node.CoerceToType(typeof(T));
        }
    }
}
