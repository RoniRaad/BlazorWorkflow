using System.Text.Json.Serialization;

namespace BlazorFlow.Models.DTOs;

public sealed record DfConnectionRef
{
    [JsonPropertyName("node")] public required int Node { get; init; }
    [JsonPropertyName("output")] public required string Output { get; init; }
    [JsonPropertyName("input")] public required string Input { get; init; }
}

