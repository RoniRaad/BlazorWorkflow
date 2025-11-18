using System.Text.Json.Serialization;

namespace BlazorExecutionFlow.Models.DTOs;

public sealed class Port
{
    [JsonPropertyName("connections")]
    public List<PortConnection> Connections { get; init; } = new();
}
