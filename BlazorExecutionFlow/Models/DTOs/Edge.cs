namespace BlazorExecutionFlow.Models.DTOs;

/// <summary>
/// Convenience edge type for graph queries
/// </summary>
public sealed class Edge
{
    public string FromNodeId { get; init; } = "";
    public string FromOutputPort { get; init; } = ""; // e.g., "output_1"
    public string ToNodeId { get; init; } = "";
    public string ToInputPort { get; init; } = "";    // e.g., "input_2"

    public override string ToString()
        => $"{FromNodeId}:{FromOutputPort} -> {ToNodeId}:{ToInputPort}";
}
