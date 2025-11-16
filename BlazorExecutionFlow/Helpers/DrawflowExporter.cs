using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BlazorExecutionFlow.Models.NodeV2;

namespace BlazorExecutionFlow.Helpers
{
    public static class DrawflowExporter
    {
        private static readonly JsonSerializerOptions NodeSerializationOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { ExcludeExecutionState }
            }
        };

        private static void ExcludeExecutionState(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Type == typeof(Node))
            {
                // Exclude execution state properties from serialization
                foreach (var property in typeInfo.Properties)
                {
                    if (property.Name == "Input" ||
                        property.Name == "Result" ||
                        property.Name == "InputNodes" ||
                        property.Name == "OutputNodes" ||
                        property.Name == "OutputPorts")
                    {
                        property.ShouldSerialize = (_, _) => false;
                    }
                }
            }
        }

        public static string ExportToDrawflowJson(IEnumerable<Node> nodes)
        {
            var nodeList = nodes.ToList();

            // Group nodes by Section => Drawflow "modules" (here: single "Home")
            var modules = new Dictionary<string, DrawflowModule>(StringComparer.Ordinal);
            const string moduleName = "Home";

            if (!modules.TryGetValue(moduleName, out var module))
            {
                module = new DrawflowModule();
                modules[moduleName] = module;
            }

            // First pass: create node entries with correct number of ports
            foreach (var node in nodeList)
            {
                var nodeKey = node.DrawflowNodeId;
                if (string.IsNullOrWhiteSpace(nodeKey))
                {
                    nodeKey = node.Id;
                }

                var idValue = int.TryParse(nodeKey, out var numericId) ? numericId : 0;

                // How many outputs? If we have declared ports, match that; else default to 1.
                var outputCount = (node.DeclaredOutputPorts is { Count: > 0 })
                    ? node.DeclaredOutputPorts.Count
                    : 1;

                var outputs = new Dictionary<string, DrawflowPortDto>(StringComparer.Ordinal);
                for (int i = 1; i <= outputCount; i++)
                {
                    outputs[$"output_{i}"] = new DrawflowPortDto();
                }

                // Prepare inputs; we'll ensure at least input_1 exists and add more as needed.
                var inputs = new Dictionary<string, DrawflowPortDto>(StringComparer.Ordinal)
                {
                    ["input_1"] = new DrawflowPortDto()
                };

                var dto = new DrawflowNodeDto
                {
                    id = idValue,
                    name = node.BackingMethod.Name,
                    @class = "",
                    html = BuildHtml(node),
                    typenode = false,
                    pos_x = node.PosX,
                    pos_y = node.PosY,
                    inputs = inputs,
                    outputs = outputs
                };

                module.data[nodeKey] = dto;
            }

            // Helper: get or create an input port id on a destination node (input_1, input_2, ...)
            static string EnsureDestInputPort(DrawflowNodeDto destDto)
            {
                // First connection can reuse "input_1"; then "input_2", "input_3", ...
                if (destDto.inputs.Count == 0)
                {
                    destDto.inputs["input_1"] = new DrawflowPortDto();
                    return "input_1";
                }

                // If "input_1" exists with no connections, reuse it
                if (destDto.inputs.TryGetValue("input_1", out var firstInput) &&
                    firstInput.connections.Count == 0)
                {
                    return "input_1";
                }

                // Otherwise, allocate a new input_N
                var idx = destDto.inputs.Count + 1;
                var key = $"input_{idx}";
                if (!destDto.inputs.ContainsKey(key))
                {
                    destDto.inputs[key] = new DrawflowPortDto();
                }
                return key;
            }

            // Second pass: wire up connections, preserving ports
            foreach (var node in nodeList)
            {
                var srcKey = string.IsNullOrWhiteSpace(node.DrawflowNodeId)
                    ? node.Id
                    : node.DrawflowNodeId;

                var srcDto = module.data[srcKey];

                // Preferred: use OutputPorts (portName -> targets)
                if (node.OutputPorts.Count > 0)
                {
                    foreach (var kvp in node.OutputPorts)
                    {
                        var portName = kvp.Key;
                        var targets = kvp.Value;

                        // Map portName -> index based on DeclaredOutputPorts
                        int index = 0;
                        if (node.DeclaredOutputPorts is { Count: > 0 })
                        {
                            var idx = node.DeclaredOutputPorts.IndexOf(portName);
                            if (idx >= 0)
                                index = idx;
                        }
                        // 0-based -> Drawflow "output_1"
                        var outputId = $"output_{index + 1}";

                        if (!srcDto.outputs.TryGetValue(outputId, out var srcOutputPort))
                        {
                            srcOutputPort = new DrawflowPortDto();
                            srcDto.outputs[outputId] = srcOutputPort;
                        }

                        foreach (var dest in targets)
                        {
                            var destKey = string.IsNullOrWhiteSpace(dest.DrawflowNodeId)
                                ? dest.Id
                                : dest.DrawflowNodeId;

                            if (!module.data.TryGetValue(destKey, out var destDto))
                                continue;

                            var destInputId = EnsureDestInputPort(destDto);
                            var destInputPort = destDto.inputs[destInputId];

                            // Source side (outputs)
                            srcOutputPort.connections.Add(new DrawflowConnectionDto
                            {
                                node = destKey,
                                // For outputs: "output": "input_1" (target input id)
                                output = destInputId
                            });

                            // Destination side (inputs)
                            destInputPort.connections.Add(new DrawflowConnectionDto
                            {
                                node = srcKey,
                                // For inputs: "input": "output_1" (source output id)
                                input = outputId
                            });
                        }
                    }
                }
                // Fallback: if no OutputPorts are set, use OutputNodes as a single default port
                else if (node.OutputNodes.Count > 0)
                {
                    const string outputId = "output_1";

                    if (!srcDto.outputs.TryGetValue(outputId, out var srcOutputPort))
                    {
                        srcOutputPort = new DrawflowPortDto();
                        srcDto.outputs[outputId] = srcOutputPort;
                    }

                    foreach (var dest in node.OutputNodes)
                    {
                        var destKey = string.IsNullOrWhiteSpace(dest.DrawflowNodeId)
                            ? dest.Id
                            : dest.DrawflowNodeId;

                        if (!module.data.TryGetValue(destKey, out var destDto))
                            continue;

                        var destInputId = EnsureDestInputPort(destDto);
                        var destInputPort = destDto.inputs[destInputId];

                        srcOutputPort.connections.Add(new DrawflowConnectionDto
                        {
                            node = destKey,
                            output = destInputId
                        });

                        destInputPort.connections.Add(new DrawflowConnectionDto
                        {
                            node = srcKey,
                            input = outputId
                        });
                    }
                }
            }

            var root = new DrawflowRoot { drawflow = modules };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(root, options);
        }

        private static string BuildHtml(Node node)
        {
            return $"""
                    <div class='node-type-id-container'>
                        <h5 class='node-type-id'>
                            F
                        </h5>
                    </div>
                    <div class='title-container'>
                        <div class='title' style='text-align: center;'>{node.BackingMethod.Name}</div>
                    </div>
                    <div class='main-content' style='min-width:300px'>
                    
                    </div>
                    """;
        }
    }

    public class DrawflowRoot
    {
        public Dictionary<string, DrawflowModule> drawflow { get; set; } = new();
    }

    public class DrawflowModule
    {
        public Dictionary<string, DrawflowNodeDto> data { get; set; } = new();
    }

    public class DrawflowNodeDto
    {
        public int id { get; set; }
        public string name { get; set; } = default!;
        public JsonObject data { get; set; } = new();
        public string @class { get; set; } = default!;
        public string html { get; set; } = default!;
        public bool typenode { get; set; }
        public Dictionary<string, DrawflowPortDto> inputs { get; set; } = new();
        public Dictionary<string, DrawflowPortDto> outputs { get; set; } = new();
        public double pos_x { get; set; }
        public double pos_y { get; set; }
    }

    public class DrawflowPortDto
    {
        public List<DrawflowConnectionDto> connections { get; set; } = new();
    }

    public class DrawflowConnectionDto
    {
        public string node { get; set; } = default!;
        public string? input { get; set; }  // used under "inputs" (source output id)
        public string? output { get; set; } // used under "outputs" (target input id)
    }
}
