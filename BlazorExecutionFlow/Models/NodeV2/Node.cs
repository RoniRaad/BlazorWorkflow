using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using BlazorExecutionFlow.Flow.Attributes;
using BlazorExecutionFlow.Helpers;
using Scriban;
using Scriban.Runtime;

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
        private readonly SemaphoreSlim _executionSemaphore = new(1);
        private bool _disposed;

        [JsonConverter(typeof(MethodInfoJsonConverter))]
        public required MethodInfo BackingMethod { get; set; }
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Section { get; set; } = string.Empty;
        public string DrawflowNodeId { get; set; } = string.Empty;
        public double PosX { get; set; }
        public double PosY { get; set; }

        public List<PathMapEntry> NodeInputToMethodInputMap { get; set; } = [];
        public List<PathMapEntry> MethodOutputToNodeOutputMap { get; set; } = [];
        public JsonObject? Input { get; set; }
        public JsonObject? Result { get; set; }
        public bool MergeOutputWithInput { get; set; } = false;

        public List<string> DeclaredOutputPorts { get; set; } = [];

        [JsonIgnore] public bool HasError { get; set; } = false;
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

        [JsonIgnore] private bool _portsInitialized;
        [JsonIgnore] private bool _isPortDriven;

        // Ports requested while this node is running
        [JsonIgnore] private readonly List<string> _pendingPortTriggers = new();
        [JsonIgnore] private readonly object _pendingPortsLock = new();

        [JsonIgnore]
        public bool IsPortDriven
        {
            get
            {
                EnsurePortsInitialized();
                return _isPortDriven;
            }
        }

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

            portName ??= "default";

            if (!OutputPorts.TryGetValue(portName, out var list))
            {
                list = [];
                OutputPorts[portName] = list;
            }

            if (!list.Contains(target))
                list.Add(target);

            if (!OutputNodes.Contains(target))
                OutputNodes.Add(target);
        }

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
        /// Caches the result for performance in iteration scenarios.
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
                if (!visited.Contains(target))
                {
                    queue.Enqueue(target);
                    visited.Add(target);
                }
            }

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                result.Add(node);

                foreach (var child in node.OutputNodes)
                {
                    if (!visited.Contains(child))
                    {
                        queue.Enqueue(child);
                        visited.Add(child);
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

            OnStartExecuting?.Invoke(this, EventArgs.Empty);

            // Clear previous errors
            HasError = false;
            ErrorMessage = null;
            LastException = null;

            await _executionSemaphore.WaitAsync();

            try
            {
                if (Result != null)
                    return Result;

                // Merge multiple upstream inputs
                var upstreamMerged = new JsonObject();

                if (InputNodes.Count > 0)
                {
                    foreach (var inputNode in InputNodes)
                    {
                        var res = await inputNode.GetResult(this);
                        upstreamMerged.Merge(res);
                    }
                }

                var formattedJsonObjectResult = new JsonObject();
                formattedJsonObjectResult.SetByPath("input", upstreamMerged.GetByPath("output"));

                Input = formattedJsonObjectResult;

                var filledMethodParameters = GetMethodParametersFromInputResult(formattedJsonObjectResult);
                Result = await InvokeBackingMethod(filledMethodParameters);

                if (MergeOutputWithInput)
                {
                    Result.Merge(upstreamMerged);
                }

                // Now that Result is set, it's safe to execute any queued port triggers
                await FlushPendingPortsAsync();

                return Result;
            }
            catch (Exception ex)
            {
                // Store error information
                HasError = true;
                ErrorMessage = $"Node '{BackingMethod?.Name ?? "Unknown"}' failed: {ex.Message}";
                LastException = ex;

                // Fire error event
                OnError?.Invoke(this, new NodeErrorEventArgs
                {
                    Exception = ex,
                    Message = ErrorMessage,
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

                return Result;
            }
            finally
            {
                _executionSemaphore.Release();
                OnStopExecuting?.Invoke(this, EventArgs.Empty);
            }
        }

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
            // Lock to prevent race condition between Result check and queue add
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
            else if (returnType.IsGenericType
                     && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var task = (Task)methodInvocationResponse!;
                await task;

                var resultProperty = task.GetType().GetProperty("Result");
                methodInvocationResponse = resultProperty?.GetValue(task);
            }

            var resultObject = new JsonObject();

            // Get the actual return type (unwrap Task<T> if needed)
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
                }
            }
            else
            {
                // Standard serialization for other types
                var serializedResponse = JsonSerializer.SerializeToNode(methodInvocationResponse);

                // Check if this is a single-value type (like JsonObject, arrays, primitives)
                // For these, we want to map the entire value to "result", not extract properties
                bool isSingleValueType = TypeHelpers.ShouldTreatAsSingleValue(actualReturnType);

                if (serializedResponse is not JsonObject methodOutputJsonObject || isSingleValueType)
                {
                    // Either not a JsonObject, or is a single-value type
                    // Map the entire serialized response to the output
                    foreach (var methodOutputMap in MethodOutputToNodeOutputMap)
                    {
                        // For single-value types, the mapping is typically "result" -> "result"
                        // Just map the whole serialized response
                        resultObject.SetByPath($"output.{methodOutputMap.To}", serializedResponse);
                    }
                }
                else
                {
                    // Complex object - extract individual properties
                    foreach (var methodOutputMap in MethodOutputToNodeOutputMap)
                    {
                        var methodOutputValue = methodOutputJsonObject.GetByPath(methodOutputMap.From);
                        resultObject.SetByPath($"output.{methodOutputMap.To}", methodOutputValue);
                    }
                }
            }

            return resultObject;
        }

        private object[] GetMethodParametersFromInputResult(JsonObject inputPayload)
        {
            var parameters = BackingMethod.GetParameters();
            var methodParameterNameToValueMap = new Dictionary<string, string?>(StringComparer.Ordinal);
            var orderedMethodParameters = new object?[parameters.Length];

            foreach (var pathKvpMap in NodeInputToMethodInputMap)
            {
                methodParameterNameToValueMap[pathKvpMap.To] = pathKvpMap.From;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (parameter.ParameterType == typeof(NodeContext))
                {
                    orderedMethodParameters[i] = new NodeContext
                    {
                        CurrentNode = this,
                        InputNodes = InputNodes,
                        OutputNodes = OutputNodes,
                        Context = [],
                        ExecutePortInternal = ExecutePortAsync
                    };
                    continue;
                }

                if (parameter.ParameterType == typeof(IServiceProvider))
                {
                    orderedMethodParameters[i] = Helpers.NodeServiceProvider.Instance;
                    continue;
                }

                methodParameterNameToValueMap.TryGetValue(parameter.Name!, out var value);

                if (value is null)
                {
                    orderedMethodParameters[i] =
                        parameter.ParameterType.IsValueType
                            ? Activator.CreateInstance(parameter.ParameterType)
                            : null;
                    continue;
                }

                // Check if value is a simple path reference (no Scriban template expressions like {{ }})
                // If so, get the JsonNode directly to preserve type information (especially for arrays/objects)
                if (!value.Contains("{{") && !value.Contains("}}"))
                {
                    var jsonValue = inputPayload.GetByPath(value);
                    if (jsonValue != null)
                    {
                        // Deserialize directly to the target type without template rendering
                        // This preserves arrays, objects, and other complex types
                        orderedMethodParameters[i] = jsonValue.CoerceToType(parameter.ParameterType);
                        continue;
                    }
                }

                // Fall back to template rendering for complex expressions
                var modelDict = inputPayload.ToPlainObject()!;

                var scriptObject = new ScriptObject();
                scriptObject.Import(modelDict);

                var context = new TemplateContext();
                context.PushGlobal(scriptObject);

                if (parameter.ParameterType == typeof(string) &&
                    value is not null &&
                    !value.StartsWith("\"") &&
                    !value.EndsWith("\""))
                {
                    value = $"\"{value}\"";
                }

                var template = Template.Parse(value);
                var result = template.Render(context);

                if (result == string.Empty)
                {
                    if (parameter.ParameterType == typeof(string))
                    {
                        orderedMethodParameters[i] = result;
                    }
                    else
                    {
                        orderedMethodParameters[i] = null;
                    }
                }
                else
                {
                    // Fix arrays/objects rendered by Scriban without proper JSON quoting
                    // e.g., [a,b,c] should be ["a","b","c"]
                    result = EnsureValidJson(result);

                    var parsedResult = ParseLiteral(result);
                    orderedMethodParameters[i] = parsedResult.CoerceToType(parameter.ParameterType);
                }
            }

            return orderedMethodParameters!;
        }

        private string EnsureValidJson(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var trimmed = input.Trim();

            // Check if it looks like an array or object
            if (!trimmed.StartsWith("[") && !trimmed.StartsWith("{"))
                return input;

            // Try to parse as-is - if it's already valid JSON, return it
            try
            {
                JsonSerializer.Deserialize<JsonNode>(trimmed);
                return input; // Already valid JSON
            }
            catch
            {
                // Not valid JSON, try to fix it
            }

            // Fix arrays with unquoted string elements: [a,b,c] -> ["a","b","c"]
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                try
                {
                    return FixArrayQuoting(trimmed);
                }
                catch
                {
                    // If we can't fix it, return original
                    return input;
                }
            }

            return input;
        }

        private string FixArrayQuoting(string arrayString)
        {
            // Remove brackets
            var content = arrayString.Substring(1, arrayString.Length - 2).Trim();

            if (string.IsNullOrEmpty(content))
                return "[]";

            // Split by comma, but respect nested structures
            var elements = SplitArrayElements(content);

            // Process each element
            var fixedElements = elements.Select(element =>
            {
                var trimmedElement = element.Trim();

                // Already quoted string
                if (trimmedElement.StartsWith("\"") && trimmedElement.EndsWith("\""))
                    return trimmedElement;

                // Nested array or object
                if (trimmedElement.StartsWith("[") || trimmedElement.StartsWith("{"))
                    return EnsureValidJson(trimmedElement);

                // Boolean literals
                if (trimmedElement == "true" || trimmedElement == "false")
                    return trimmedElement;

                // Null literal
                if (trimmedElement == "null")
                    return trimmedElement;

                // Number (int or decimal)
                if (double.TryParse(trimmedElement, out _))
                    return trimmedElement;

                // Otherwise, treat as unquoted string and quote it
                return JsonSerializer.Serialize(trimmedElement);
            });

            return $"[{string.Join(",", fixedElements)}]";
        }

        private List<string> SplitArrayElements(string content)
        {
            var elements = new List<string>();
            var currentElement = new StringBuilder();
            var depth = 0;
            var inString = false;
            var escapeNext = false;

            foreach (var ch in content)
            {
                if (escapeNext)
                {
                    currentElement.Append(ch);
                    escapeNext = false;
                    continue;
                }

                if (ch == '\\')
                {
                    currentElement.Append(ch);
                    escapeNext = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = !inString;
                    currentElement.Append(ch);
                    continue;
                }

                if (!inString)
                {
                    if (ch == '[' || ch == '{')
                    {
                        depth++;
                        currentElement.Append(ch);
                        continue;
                    }

                    if (ch == ']' || ch == '}')
                    {
                        depth--;
                        currentElement.Append(ch);
                        continue;
                    }

                    if (ch == ',' && depth == 0)
                    {
                        // End of current element
                        elements.Add(currentElement.ToString());
                        currentElement.Clear();
                        continue;
                    }
                }

                currentElement.Append(ch);
            }

            // Add the last element
            if (currentElement.Length > 0)
            {
                elements.Add(currentElement.ToString());
            }

            return elements;
        }

        private JsonNode? ParseLiteral(string input)
        {
            var json = input;
            var trimmed = input.TrimStart();

            if (trimmed.Length > 0 &&
                !trimmed.StartsWith("{") &&
                !trimmed.StartsWith("[") &&
                !trimmed.StartsWith("\"") &&
                !char.IsDigit(trimmed[0]) &&
                !trimmed.StartsWith("-") &&  // Allow negative numbers
                !trimmed.StartsWith("+") &&  // Allow explicit positive numbers
                !"tfn".Contains(char.ToLowerInvariant(trimmed[0])))
            {
                json = JsonSerializer.Serialize(input);
            }

            return JsonSerializer.Deserialize<JsonNode>(json);
        }

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
                _executionSemaphore?.Dispose();

                // Clear event handlers to prevent memory leaks
                OnStartExecuting = null;
                OnStopExecuting = null;
                OnError = null;
            }

            _disposed = true;
        }
    }

    public class PathMapEntry
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
    }
}
