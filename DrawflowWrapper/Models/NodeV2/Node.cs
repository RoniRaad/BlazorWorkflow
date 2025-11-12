using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using DrawflowWrapper.Drawflow.Attributes;
using DrawflowWrapper.Helpers;
using Scriban;
using Scriban.Runtime;

namespace DrawflowWrapper.Models.NodeV2
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

        public event EventHandler? OnStartExecuting;
        public event EventHandler? OnStopExecuting;

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

            await _executionSemaphore.WaitAsync();

            try
            {
                if (Result != null)
                    return Result;

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

            var serializedResponse = JsonSerializer.SerializeToNode(methodInvocationResponse);

            var resultObject = new JsonObject();

            if (serializedResponse is not JsonObject methodOutputJsonObject)
            {
                resultObject.SetByPath("output.result", serializedResponse);
            }
            else
            {
                foreach (var methodOutputMap in MethodOutputToNodeOutputMap)
                {
                    var methodOutputValue = methodOutputJsonObject.GetByPath(methodOutputMap.From);
                    resultObject.SetByPath($"output.{methodOutputMap.To}", methodOutputValue);
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

                methodParameterNameToValueMap.TryGetValue(parameter.Name!, out var value);

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

                if (value is null)
                {
                    orderedMethodParameters[i] =
                        parameter.ParameterType.IsValueType
                            ? Activator.CreateInstance(parameter.ParameterType)
                            : null;
                    continue;
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
                    var parsedResult = ParseLiteral(result);
                    orderedMethodParameters[i] = parsedResult.CoerceToType(parameter.ParameterType);
                }
            }

            return orderedMethodParameters!;
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
