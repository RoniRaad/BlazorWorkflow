using System.Text.Json.Serialization;

namespace BlazorFlow.Models.DTOs;

public sealed record DfPort
{
    [JsonPropertyName("connections")] public List<DfConnectionRef> Connections { get; init; } = new();
}
