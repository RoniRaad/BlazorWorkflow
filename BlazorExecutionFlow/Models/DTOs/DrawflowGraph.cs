using System.Text.Json;
using BlazorExecutionFlow.Components;

namespace BlazorExecutionFlow.Models.DTOs;

/// <summary>
/// Graph wrapper with parsing + helpers
/// </summary>
public sealed class DrawflowGraph
{
    public DrawflowDocument Document { get; }
    public string PageName { get; }
    public DrawflowPage Page { get; }
    public BlazorExecutionFlowGraphBase DrawflowBase { get; set; }

    // Cached quick-lookups
    private readonly Dictionary<string, DrawflowNode> _nodes;
    private readonly List<Edge> _edges;

    private DrawflowGraph(DrawflowDocument doc, string pageName, DrawflowPage page)
    {
        Document = doc;
        PageName = pageName;
        Page = page;

        _nodes = page.Data; // keys are string ids ("1", "2", ...)
        _edges = BuildEdges(page);
    }

    /// <summary>
    /// Parse Drawflow JSON and focus on a page (default: "Home").
    /// </summary>
    public static DrawflowGraph Parse(BlazorExecutionFlowGraphBase dfBase, string json, string pageName = "Home")
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        var doc = JsonSerializer.Deserialize<DrawflowDocument>(json, options)
                  ?? throw new InvalidOperationException("Failed to deserialize Drawflow JSON.");

        var page = doc.GetPage(pageName)
                   ?? throw new KeyNotFoundException($"Page '{pageName}' not found in drawflow.");

        var graph = new DrawflowGraph(doc, pageName, page)
        {
            DrawflowBase = dfBase
        };

        return graph;
    }

    /// <summary>Get all node ids on the page.</summary>
    public IEnumerable<string> NodeIds() => _nodes.Keys;

    /// <summary>Try get node by id ("1", "2", ...).</summary>
    public bool TryGetNode(string nodeId, out DrawflowNode? node) =>
        _nodes.TryGetValue(nodeId, out node);

    /// <summary>Get node by id or throw.</summary>
    public DrawflowNode GetNode(string nodeId) =>
        _nodes.TryGetValue(nodeId, out var node)
            ? node
            : throw new KeyNotFoundException($"Node '{nodeId}' not found.");

    /// <summary>All edges discovered on the page.</summary>
    public IReadOnlyList<Edge> Edges => _edges;

    /// <summary>Edges where from = nodeId.</summary>
    public IEnumerable<Edge> GetOutgoing(string nodeId) =>
        _edges.Where(e => e.FromNodeId == nodeId);

    /// <summary>Edges where to = nodeId.</summary>
    public IEnumerable<Edge> GetIncoming(string nodeId) =>
        _edges.Where(e => e.ToNodeId == nodeId);

    /// <summary>All edges from A to B (any ports).</summary>
    public IEnumerable<Edge> GetEdgesBetween(string fromNodeId, string toNodeId) =>
        _edges.Where(e => e.FromNodeId == fromNodeId && e.ToNodeId == toNodeId);

    /// <summary>Find one exact connection (by node ids and port ids).</summary>
    public Edge? GetConnection(string fromNodeId, string fromOutputPort, string toNodeId, string toInputPort) =>
        _edges.FirstOrDefault(e =>
            e.FromNodeId == fromNodeId &&
            e.FromOutputPort == fromOutputPort &&
            e.ToNodeId == toNodeId &&
            e.ToInputPort == toInputPort);

    // Build edges by inspecting OUTPUTS (authoritative in Drawflow)
    private static List<Edge> BuildEdges(DrawflowPage page)
    {
        var edges = new List<Edge>();

        foreach (var (nodeId, node) in page.Data)
        {
            foreach (var (outPortName, port) in node.Outputs)
            {
                // Each connection on an output points to a target node + target input port name
                foreach (var conn in port.Connections)
                {
                    if (string.IsNullOrWhiteSpace(conn.Node) || string.IsNullOrWhiteSpace(conn.Output))
                        continue;

                    edges.Add(new Edge
                    {
                        FromNodeId = nodeId,
                        FromOutputPort = outPortName,   // "output_1"
                        ToNodeId = conn.Node,            // e.g., "2"
                        ToInputPort = conn.Output,        // "input_1"
                    });
                }
            }
        }

        return edges;
    }
}
