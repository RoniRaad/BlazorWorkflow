using System.Text.Json.Serialization;

namespace BlazorExecutionFlow.Models.DTOs;

public sealed class DrawflowNode
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("data")] public Dictionary<string, object>? Data { get; init; }
    [JsonPropertyName("class")] public string? Class { get; init; }
    [JsonPropertyName("html")] public string? Html { get; init; }
    [JsonPropertyName("typenode")] public bool TypeNode { get; init; }

    // input_N / output_N -> Port
    [JsonPropertyName("inputs")] public Dictionary<string, Port> Inputs { get; init; } = new();
    [JsonPropertyName("outputs")] public Dictionary<string, Port> Outputs { get; init; } = new();

    [JsonPropertyName("pos_x")] public double PosX { get; init; }
    [JsonPropertyName("pos_y")] public double PosY { get; init; }
}
