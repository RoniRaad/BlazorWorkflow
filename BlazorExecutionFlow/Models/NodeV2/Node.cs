using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BlazorExecutionFlow.Flow.Attributes;
using BlazorExecutionFlow.Flow.BaseNodes;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorExecutionFlow.Models.NodeV2
{
    public class NodeContext
    {
        public Node CurrentNode { get; set; } = null!;
        public IReadOnlyList<Node> InputNodes { get; set; } = Array.Empty<Node>();
        public IReadOnlyList<Node> OutputNodes { get; set; } = Array.Empty<Node>();
        public Dictionary<string, object?> Context { get; set; } = [];

        internal Func<string, Task>? ExecutePortInternal { get; set; }

        public Task ExecutePortAsync(string portName)
            => ExecutePortInternal?.Invoke(portName) ?? Task.CompletedTask;
    }

    public class NodeErrorEventArgs : EventArgs
    {
        public required Exception Exception { get; init; }
        public required string Message { get; init; }
        public required Node Node { get; init; }
    }

    public class Node : IDisposable
    {
        private const string DefaultPortName = "default";

        private readonly SemaphoreSlim _executionSemaphore = new(1);
        private readonly List<string> _pendingPortTriggers = new();
        private readonly object _pendingPortsLock = new();

        private bool _disposed;
        private bool _portsInitialized;
        private bool _isPortDriven;

        [JsonConverter(typeof(MethodInfoJsonConverter))]
        public required MethodInfo BackingMethod { get; set; }

        [JsonIgnore]
        public string Name => NameOverride ?? BackingMethod.Name;

        public bool IsWorkflowNode =>
            BackingMethod.Name == nameof(WorkflowHelpers.ExecuteWorkflow)
            && BackingMethod == typeof(WorkflowHelpers).GetMethod(nameof(WorkflowHelpers.ExecuteWorkflow));

        public string? NameOverride { get; set; }
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Section { get; set; } = string.Empty;
        public string DrawflowNodeId { get; set; } = string.Empty;
        public double PosX { get; set; }
        public double PosY { get; set; }

        [JsonIgnore]
        public GraphExecutionContext? SharedExecutionContext { get; set; }

        public List<PathMapEntry> NodeInputToMethodInputMap { get; set; } = [];
        public List<PathMapEntry> MethodOutputToNodeOutputMap { get; set; } = [];

        // For Dictionary<string, string> parameters: maps parameter name -> list of key-value mappings
        public Dictionary<string, List<PathMapEntry>> DictionaryParameterMappings { get; set; } = new();

        [JsonIgnore]
        public JsonObject? Input { get; set; }

        [JsonIgnore]
        public JsonObject? Result { get; set; }

        public string? ParentWorkflowId { get; set; }
        public bool MergeOutputWithInput { get; set; }

        public List<string> DeclaredOutputPorts { get; set; } = [];

        [JsonIgnore] public bool HasError { get; set; }
        [JsonIgnore] public string? ErrorMessage { get; set; }
        [JsonIgnore] public Exception? LastException { get; set; }

        public event EventHandler? OnStartExecuting;
        public event EventHandler? OnStopExecuting;
        public event EventHandler<NodeErrorEventArgs>? OnError;

        [JsonIgnore] public List<Node> InputNodes { get; set; } = [];
        [JsonIgnore] public List<Node> OutputNodes { get; set; } = [];

        [JsonIgnore]
        public Dictionary<string, List<Node>> OutputPorts { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool IsPortDriven
        {
            get
            {
                EnsurePortsInitialized();
                return _isPortDriven;
            }
        }

        #region Port Setup / Connections

        private void EnsurePortsInitialized()
        {
            if (_portsInitialized)
                return;

            var attr = BackingMethod.GetCustomAttribute<NodeFlowPortsAttribute>();
            if (attr != null)
            {
                _isPortDriven = true;

                if (DeclaredOutputPorts.Count == 0 && attr.Ports.Count > 0)
                {
                    DeclaredOutputPorts = attr.Ports.ToList();
                }
            }
            else
            {
                _isPortDriven = false;
            }

            _portsInitialized = true;
        }

        public void AddOutputConnection(string? portName, Node target)
        {
            EnsurePortsInitialized();

            portName ??= DefaultPortName;

            if (!OutputPorts.TryGetValue(portName, out var list))
            {
                list = [];
                OutputPorts[portName] = list;
            }

            if (!list.Contains(target))
            {
                list.Add(target);
            }

            if (!OutputNodes.Contains(target))
            {
                OutputNodes.Add(target);
            }
        }

        #endregion

        #region Clear / Graph Utilities

        /// <summary>
        /// Clears the cached result of this node, allowing it to be re-executed.
        /// Useful for iteration scenarios where a node needs to run multiple times.
        /// </summary>
        public void ClearResult()
        {
            Result = null;
            Input = null;
            HasError = false;
            ErrorMessage = null;
            LastException = null;
        }

        /// <summary>
        /// Recursively clears results for this node and all downstream nodes connected to a specific port.
        /// This allows re-execution of a subgraph.
        /// </summary>
        public void ClearDownstreamResults(string? portName = null)
        {
            var targets = portName == null
                ? OutputNodes
                : (OutputPorts.TryGetValue(portName, out var list) ? list : new List<Node>());

            foreach (var target in targets)
            {
                target.ClearResult();
                target.ClearDownstreamResults(); // Recursively clear
            }
        }

        /// <summary>
        /// Gets all downstream nodes reachable from a specific port (or all ports).
        /// </summary>
        public List<Node> GetDownstreamNodes(string? portName = null)
        {
            var result = new List<Node>();
            var visited = new HashSet<Node>();
            var queue = new Queue<Node>();

            var targets = portName == null
                ? OutputNodes
                : (OutputPorts.TryGetValue(portName, out var list) ? list : new List<Node>());

            foreach (var target in targets)
            {
                if (visited.Add(target))
                {
                    queue.Enqueue(target);
                }
            }

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                result.Add(node);

                foreach (var child in node.OutputNodes)
                {
                    if (visited.Add(child))
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Fast clear of specific nodes without recursion.
        /// Use this when you've already identified which nodes to clear.
        /// </summary>
        public static void ClearNodes(List<Node> nodes)
        {
            foreach (var node in nodes)
            {
                node.ClearResult();
            }
        }

        #endregion

        #region Execution

        public async Task ExecuteNode(Node? caller = null)
        {
            await GetResult(caller ?? this);

            // Non-port-driven nodes keep the old linear fan-out behavior
            if (!IsPortDriven)
            {
                foreach (var outputNode in OutputNodes)
                {
                    await outputNode.ExecuteNode(this);
                }
            }
        }

        public async Task<JsonObject> GetResult(Node caller)
        {
            if (Result != null)
                return Result;

            await _executionSemaphore.WaitAsync();

            try
            {
                if (Result != null)
                    return Result;

                OnStartExecuting?.Invoke(this, EventArgs.Empty);
                ResetErrorState();

                // Merge of all input data
                var inputNodesData = await BuildInputNodesDataAsync();

                // Input nodes output data is our input data
                var formattedInput = BuildFormattedInput(inputNodesData);
                Input = formattedInput;

                if (IsWorkflowNode)
                {
                    Result = await ExecuteAsWorkflowNodeAsync(formattedInput);
                }
                else
                {
                    var filledMethodParameters = GetMethodParametersFromInputResult(formattedInput);
                    Result = await InvokeBackingMethod(filledMethodParameters);
                }

                PropagateWorkflowOutputToSharedContext(Result);

                if (MergeOutputWithInput && Result != null)
                {
                    Result.Merge(inputNodesData);
                }

                // Now that Result is set, it's safe to execute any queued port triggers
                await FlushPendingPortsAsync();

                return Result ?? [];
            }
            catch (Exception ex)
            {
                HandleExecutionException(ex);
                return Result!;
            }
            finally
            {
                _executionSemaphore.Release();
                OnStopExecuting?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ResetErrorState()
        {
            HasError = false;
            ErrorMessage = null;
            LastException = null;
        }

        private async Task<JsonObject> BuildInputNodesDataAsync()
        {
            var inputNodesData = new JsonObject();

            if (InputNodes.Count == 0)
                return inputNodesData;

            foreach (var inputNode in InputNodes)
            {
                var res = await inputNode.GetResult(this);
                inputNodesData.Merge(res);
            }

            return inputNodesData;
        }

        private static JsonObject BuildFormattedInput(JsonObject inputNodesData)
        {
            var formattedJsonObjectResult = new JsonObject();
            formattedJsonObjectResult.SetByPath("input", inputNodesData.GetByPath("output"));
            return formattedJsonObjectResult;
        }

        private async Task<JsonObject> ExecuteAsWorkflowNodeAsync(JsonObject formattedInput)
        {
            var workflowService = NodeServiceProvider.Instance?.GetService<IWorkflowService>();
            var workflow = workflowService?.GetWorkflow(ParentWorkflowId);

            var jsonObject = new JsonObject();
            if (workflow is null)
                return jsonObject;

            Dictionary<string, string> mappedValues = [];
            var nameToPathMap = BuildMethodParameterNameToValueMap();

            foreach (var param in NodeInputToMethodInputMap)
            {
                var value = CreateScribanParameterValue(
                    typeof(string),
                    param.To,
                    nameToPathMap,
                    formattedInput
                );

                mappedValues[param.To] = value as string ?? string.Empty;
            }

            var environment = SharedExecutionContext?.EnvironmentVariables?.ToDictionary() ?? [];
            var result = await WorkflowHelpers.ExecuteWorkflow(workflow, mappedValues, environment);

            result.Remove("environment");
            jsonObject.SetByPath($"output.external_workflows.{workflow.Id}", result);

            return jsonObject;
        }

        private void PropagateWorkflowOutputToSharedContext(JsonObject? result)
        {
            if (result == null || SharedExecutionContext?.SharedContext == null)
                return;

            var outputResults = result.GetByPath("output.workflow.output");
            if (outputResults is JsonObject outputResultsObject)
            {
                SharedExecutionContext.SharedContext.Merge(outputResultsObject);
            }
        }

        private void HandleExecutionException(Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Node '{BackingMethod?.Name ?? "Unknown"}' failed: {ex.Message}";
            LastException = ex;

            OnError?.Invoke(this, new NodeErrorEventArgs
            {
                Exception = ex,
                Message = ErrorMessage!,
                Node = this
            });

            // Create an error result so downstream nodes can continue if needed
            Result = new JsonObject
            {
                ["error"] = new JsonObject
                {
                    ["message"] = ErrorMessage,
                    ["nodeId"] = DrawflowNodeId,
                    ["nodeName"] = BackingMethod?.Name ?? "Unknown",
                    ["timestamp"] = DateTime.UtcNow.ToString("o")
                }
            };
        }

        #endregion

        #region Port Execution

        // Called by NodeContext
        internal Task ExecutePortAsync(string portName)
        {
            EnsurePortsInitialized();

            if (!_isPortDriven)
            {
                // Old behavior: just fan-out to all outputs
                return ExecutePortNowAsync(portName);
            }

            if (string.IsNullOrWhiteSpace(portName))
                return Task.CompletedTask;

            // If we don't have a Result yet, we're still executing this node.
            // Queue the port and run it after Result is ready to avoid deadlock.
            lock (_pendingPortsLock)
            {
                if (Result == null)
                {
                    _pendingPortTriggers.Add(portName);
                    // fire-and-forget semantics while node is running
                    return Task.CompletedTask;
                }
            }

            // Node already finished: execute immediately
            return ExecutePortNowAsync(portName);
        }

        private async Task FlushPendingPortsAsync()
        {
            List<string> ports;
            lock (_pendingPortsLock)
            {
                if (_pendingPortTriggers.Count == 0)
                    return;

                ports = new List<string>(_pendingPortTriggers);
                _pendingPortTriggers.Clear();
            }

            foreach (var port in ports)
            {
                await ExecutePortNowAsync(port);
            }
        }

        private async Task ExecutePortNowAsync(string portName)
        {
            EnsurePortsInitialized();

            // Non-port-driven: execute all outputs
            if (!_isPortDriven)
            {
                foreach (var output in OutputNodes)
                {
                    await output.ExecuteNode(this);
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(portName))
                return;

            if (!OutputPorts.TryGetValue(portName, out var targets) || targets.Count == 0)
                return;

            foreach (var target in targets)
            {
                await target.ExecuteNode(this);
            }
        }

        #endregion

        #region Backing Method Invocation / Parameters

        private async Task<JsonObject> InvokeBackingMethod(object[] filledMethodParameters)
        {
            if (BackingMethod == null)
                throw new InvalidOperationException("BackingMethod is null");

            if (BackingMethod.DeclaringType == null)
                throw new InvalidOperationException($"BackingMethod {BackingMethod.Name} has no DeclaringType");

            var returnType = BackingMethod.ReturnType;
            var methodInvocationResponse = BackingMethod.Invoke(null, filledMethodParameters);

            if (returnType == typeof(void))
            {
                methodInvocationResponse = null;
                return [];
            }

            if (returnType == typeof(Task))
            {
                await ((Task)methodInvocationResponse!);
                methodInvocationResponse = null;
            }
            else if (returnType.IsGenericType &&
                     returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var task = (Task)methodInvocationResponse!;
                await task;

                var resultProperty = task.GetType().GetProperty("Result");
                methodInvocationResponse = resultProperty?.GetValue(task);
            }

            var resultObject = new JsonObject();
            var actualReturnType = TypeHelpers.UnwrapTaskType(returnType);

            // Check if this type has curated properties that need special handling
            var curatedProperties = TypeHelpers.GetCuratedProperties(actualReturnType);
            if (curatedProperties != null && methodInvocationResponse != null)
            {
                // For types with curated properties (like DateTime), extract properties using reflection
                var methodOutputJsonObject = new JsonObject();
                var responseType = methodInvocationResponse.GetType();

                foreach (var prop in curatedProperties)
                {
                    if (prop.Name != null)
                    {
                        var propertyInfo = responseType.GetProperty(prop.Name);
                        if (propertyInfo != null)
                        {
                            var propertyValue = propertyInfo.GetValue(methodInvocationResponse);
                            methodOutputJsonObject[prop.Name] = JsonSerializer.SerializeToNode(propertyValue);
                        }
                    }
                }

                // Map curated properties to outputs
                foreach (var methodOutputMap in MethodOutputToNodeOutputMap)
                {
                    var methodOutputValue = methodOutputJsonObject.GetByPath(methodOutputMap.From);
                    resultObject.SetByPath($"output.{methodOutputMap.To}", methodOutputValue);

                    SharedExecutionContext?.SharedContext.SetByPath(
                        $"nodes.node_{DrawflowNodeId}.output",
                        methodOutputValue?.DeepClone());

                    SharedExecutionContext?.SharedContext.SetByPath(
                        $"nodes.node_{DrawflowNodeId}.name",
                        BackingMethod.Name);

                    // Also expose to workflow.output.* if flagged
                    if (methodOutputMap.ExposeAsWorkflowOutput)
                    {
                        SharedExecutionContext?.SharedContext.SetByPath(
                            $"workflow.output.{methodOutputMap.To}",
                            methodOutputValue?.DeepClone());
                    }
                }
            }
            else
            {
                // Standard serialization for other types
                var serializedResponse = JsonSerializer.SerializeToNode(methodInvocationResponse);

                // If the response is a string that contains valid JSON, parse it
                // This allows string results (like HTTP responses) to be indexed like JSON
                if (serializedResponse is JsonValue jsonValue &&
                    jsonValue.GetValueKind() == JsonValueKind.String)
                {
                    var stringValue = jsonValue.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(stringValue))
                    {
                        try
                        {
                            // Try to parse the string as JSON
                            var parsed = JsonNode.Parse(stringValue);
                            if (parsed != null)
                            {
                                serializedResponse = parsed;
                            }
                        }
                        catch
                        {
                            // Not valid JSON, keep as string
                        }
                    }
                }

                // Check if this is a single-value type (like JsonObject, arrays, primitives)
                // For these, we want to map the entire value to "result", not extract properties
                // However, if we've parsed a JSON string into an object, treat it as a complex object
                bool isSingleValueType =
                    TypeHelpers.ShouldTreatAsSingleValue(actualReturnType) &&
                    serializedResponse is not JsonObject;

                if (serializedResponse is not JsonObject methodOutputJsonObject || isSingleValueType)
                {
                    // Either not a JsonObject, or is a single-value type
                    // Map the entire serialized response to the output
                    foreach (var methodOutputMap in MethodOutputToNodeOutputMap)
                    {
                        resultObject.SetByPath($"output.{methodOutputMap.To}", serializedResponse);

                        SharedExecutionContext?.SharedContext.SetByPath(
                            $"nodes.node_{DrawflowNodeId}.output",
                            serializedResponse?.DeepClone());

                        SharedExecutionContext?.SharedContext.SetByPath(
                            $"nodes.node_{DrawflowNodeId}.name",
                            BackingMethod.Name);

                        // Also expose to workflow.output.* if flagged
                        if (methodOutputMap.ExposeAsWorkflowOutput)
                        {
                            SharedExecutionContext?.SharedContext.SetByPath(
                                $"workflow.output.{methodOutputMap.To}",
                                serializedResponse?.DeepClone());
                        }
                    }
                }
                else
                {
                    // Complex object - expose entire object to mapped outputs
                    foreach (var methodOutputMap in MethodOutputToNodeOutputMap)
                    {
                        resultObject.SetByPath($"output.{methodOutputMap.To}", methodOutputJsonObject);

                        SharedExecutionContext?.SharedContext.SetByPath(
                            $"nodes.node_{DrawflowNodeId}.output",
                            methodOutputJsonObject?.DeepClone());

                        SharedExecutionContext?.SharedContext.SetByPath(
                            $"nodes.node_{DrawflowNodeId}.name",
                            BackingMethod.Name);

                        // Also expose to workflow.output.* if flagged
                        if (methodOutputMap.ExposeAsWorkflowOutput)
                        {
                            SharedExecutionContext?.SharedContext.SetByPath(
                                $"workflow.output.{methodOutputMap.To}",
                                methodOutputJsonObject?.DeepClone());
                        }
                    }
                }
            }

            return resultObject;
        }

        private object[] GetMethodParametersFromInputResult(JsonObject inputPayload)
        {
            var parameters = BackingMethod.GetParameters();
            var methodParameterNameToValueMap = BuildMethodParameterNameToValueMap();
            var orderedMethodParameters = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                orderedMethodParameters[i] = CreateParameterValue(parameter, methodParameterNameToValueMap, inputPayload);
            }

            return orderedMethodParameters!;
        }

        private Dictionary<string, string?> BuildMethodParameterNameToValueMap()
        {
            var methodParameterNameToValueMap = new Dictionary<string, string?>(StringComparer.Ordinal);

            foreach (var pathKvpMap in NodeInputToMethodInputMap)
            {
                methodParameterNameToValueMap[pathKvpMap.To] = pathKvpMap.From;
            }

            return methodParameterNameToValueMap;
        }

        private object? CreateParameterValue(
            ParameterInfo parameter,
            Dictionary<string, string?> methodParameterNameToValueMap,
            JsonObject inputPayload)
        {
            if (parameter.ParameterType == typeof(NodeContext))
            {
                return CreateNodeContext();
            }

            if (parameter.ParameterType == typeof(IServiceProvider))
            {
                return NodeServiceProvider.Instance;
            }

            if (parameter.ParameterType == typeof(Dictionary<string, string>))
            {
                return CreateDictionaryParameter(parameter, inputPayload);
            }

            return CreateScribanParameterValue(parameter, methodParameterNameToValueMap, inputPayload);
        }

        private NodeContext CreateNodeContext()
        {
            // Create context dictionary (can be enriched with SharedExecutionContext if needed)
            var contextDict = new Dictionary<string, object?>();

            return new NodeContext
            {
                CurrentNode = this,
                InputNodes = InputNodes,
                OutputNodes = OutputNodes,
                Context = contextDict,
                ExecutePortInternal = ExecutePortAsync
            };
        }

        private Dictionary<string, string> CreateDictionaryParameter(
            ParameterInfo parameter,
            JsonObject inputPayload)
        {
            var dictionary = new Dictionary<string, string>();

            if (parameter.Name != null &&
                DictionaryParameterMappings.TryGetValue(parameter.Name, out var dictMappings))
            {
                foreach (var mapping in dictMappings)
                {
                    if (string.IsNullOrWhiteSpace(mapping.To)) // "To" is the dictionary key
                        continue;

                    string? dictValue = null;

                    if (!string.IsNullOrWhiteSpace(mapping.From))
                    {
                        var result = ScribanHelpers.GetScribanObject(
                            mapping.From,
                            inputPayload,
                            SharedExecutionContext ?? new(),
                            parameter.ParameterType);

                        dictValue = result?.ToString();
                    }

                    dictionary[mapping.To] = dictValue ?? string.Empty;
                }
            }

            return dictionary;
        }

        public object? CreateScribanParameterValue(
            ParameterInfo parameter,
            Dictionary<string, string?> methodParameterNameToValueMap,
            JsonObject inputPayload)
        {
            return CreateScribanParameterValue(
                parameter.ParameterType,
                parameter.Name!,
                methodParameterNameToValueMap,
                inputPayload
            );
        }

        public object? CreateScribanParameterValue(
            Type paramType,
            string paramName,
            Dictionary<string, string?> methodParameterNameToValueMap,
            JsonObject inputPayload)
        {
            methodParameterNameToValueMap.TryGetValue(paramName, out var value);

            return ScribanHelpers.GetScribanObject(
                value,
                inputPayload,
                SharedExecutionContext ?? new(),
                paramType);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _executionSemaphore.Dispose();

                // Clear event handlers to prevent memory leaks
                OnStartExecuting = null;
                OnStopExecuting = null;
                OnError = null;
            }

            _disposed = true;
        }

        #endregion
    }

    public class PathMapEntry
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public bool ExposeAsWorkflowOutput { get; set; }
    }
}
