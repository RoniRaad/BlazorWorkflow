using System.Text.Json.Serialization;

namespace BlazorExecutionFlow.Models.DTOs;

public sealed class PortConnection
{
    // NOTE: Drawflow uses strings for these ids in JSON; keep them as-is.
    [JsonPropertyName("node")] public string Node { get; init; } = "";
    // For outputs: "output": "input_1" (target input on the other node)
    [JsonPropertyName("output")] public string? Output { get; init; }
    // For inputs: "input": "output_1" (source output on the other node)
    [JsonPropertyName("input")] public string? Input { get; init; }
}
