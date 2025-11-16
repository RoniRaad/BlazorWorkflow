using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorFlow.Models.DTOs;
public class DfNode
{
    [JsonPropertyName("id")] public required int Id { get; init; }
    [JsonPropertyName("name")] public required string Name { get; init; }
    [JsonPropertyName("data")] public required JsonElement Data { get; init; }
    [JsonPropertyName("class")] public string? Class { get; init; }
    [JsonPropertyName("html")] public string? Html { get; init; }
    [JsonPropertyName("typenode")] public bool? TypeNode { get; init; }
    [JsonPropertyName("inputs")] public Dictionary<string, DfPort>? Inputs { get; init; }
    [JsonPropertyName("outputs")] public Dictionary<string, DfPort>? Outputs { get; init; }
    [JsonPropertyName("pos_x")] public double PosX { get; init; }
    [JsonPropertyName("pos_y")] public double PosY { get; init; }
}
