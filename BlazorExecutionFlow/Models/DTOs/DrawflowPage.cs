using System.Text.Json.Serialization;

namespace BlazorExecutionFlow.Models.DTOs;

public sealed class DrawflowPage
{
    [JsonPropertyName("data")]
    public Dictionary<string, DrawflowNode> Data { get; init; } = new();
}
