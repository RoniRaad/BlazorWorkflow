using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using DrawflowWrapper.Helpers;

namespace DrawflowWrapper.Models.NodeV2
{
    public class Node
    {
        private readonly SemaphoreSlim _executionSemaphore = new(1);
        public required MethodInfo BackingMethod { get; set; }
        public List<Node> InputNodes { get; set; } = [];
        public List<Node> OutputNodes { get; set; } = [];
        public Dictionary<string, string> NodeInputToMethodInputMap { get; set; } = [];
        public Dictionary<string, string> MethodOutputToNodeOutputMap { get; set; } = [];
        public NodeContext NodeContext { get; set; } = new();
        public JsonObject? Result { get; set; }

        public async Task<JsonObject> GetResult()
        {
            if (Result != null)
                return Result;

            JsonObject? inputPayload = null;

            if (InputNodes.Count == 1)
            {
                inputPayload = await InputNodes[0].GetResult().ConfigureAwait(false);
            }
            else if (InputNodes.Count > 1)
            {
                throw new NotImplementedException("Multiple input nodes not implemented yet.");
            }

            await _executionSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (Result != null)
                    return Result;

                var parameters = BackingMethod.GetParameters();
                var methodParameterNameToValueMap = new Dictionary<string, object?>(StringComparer.Ordinal);
                var orderedMethodParameters = new object?[parameters.Length];

                if (inputPayload != null)
                {
                    foreach (var pathKvpMap in NodeInputToMethodInputMap)
                    {
                        var inputObject = inputPayload.GetByPath(pathKvpMap.Key);
                        methodParameterNameToValueMap[pathKvpMap.Value] = inputObject; // or deserialize to parameter type
                    }
                }

                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];

                    if (parameter.ParameterType == typeof(NodeContext))
                    {
                        orderedMethodParameters[i] = NodeContext;
                        continue;
                    }

                    methodParameterNameToValueMap.TryGetValue(parameter.Name!, out var value);
                    orderedMethodParameters[i] = value;
                }

                var returnType = BackingMethod.ReturnType;
                var methodInvocationResponse = BackingMethod.Invoke(null, orderedMethodParameters);

                if (returnType == typeof(void))
                {
                    methodInvocationResponse = null;
                }
                if (returnType == typeof(Task))
                {
                    await ((Task)methodInvocationResponse!).ConfigureAwait(false);
                    methodInvocationResponse = null;
                }
                else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var task = (Task)methodInvocationResponse!;
                    await task.ConfigureAwait(false);

                    var resultProperty = task.GetType().GetProperty("Result");
                    methodInvocationResponse = resultProperty?.GetValue(task);
                }

                var methodOutputJsonObject = JsonSerializer.SerializeToNode(methodInvocationResponse) as JsonObject
                    ?? throw new InvalidOperationException("Backing method output must serialize to a JsonObject.");

                var resultObject = new JsonObject();

                foreach (var methodOutputMap in MethodOutputToNodeOutputMap)
                {
                    var methodOutputValue = methodOutputJsonObject.GetByPath(methodOutputMap.Key);
                    resultObject.SetByPath(methodOutputMap.Value, methodOutputValue);
                }

                Result = resultObject;
                return Result;
            }
            finally
            {
                _executionSemaphore.Release();
            }
        }
    }
}
