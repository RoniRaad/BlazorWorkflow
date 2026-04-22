using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using BlazorWorkflow.Helpers;
using BlazorWorkflow.Models;
using BlazorWorkflow.Models.DTOs;
using BlazorWorkflow.Models.NodeV2;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorWorkflow.Components;

public class DrawflowEventArgs : EventArgs
{
    public required string Name { get; init; }
    /// <summary>Raw JSON payload array from Drawflow event arguments.</summary>
    public required string PayloadJson { get; init; }

    /// <summary>Deserialize the payload as T.</summary>
    public T? GetPayload<T>() => JsonSerializer.Deserialize<T>(PayloadJson);
}

public partial class WorkflowGraph : ComponentBase, IAsyncDisposable
{
    private DotNetObjectReference<WorkflowGraph>? _selfRef;
    private bool _created;

    [Inject] public IJSRuntime JS { get; set; } = default!;

    /// <summary>DOM element id for this editor host.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Inline style (height/width). Default "height:500px;".</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Arbitrary options passed to Drawflow constructor.</summary>
    [Parameter] public Dictionary<string, object>? Options { get; set; }

    /// <summary>Fires for every Drawflow event name that is observed.</summary>
    [Parameter] public EventCallback<DrawflowEventArgs> OnEvent { get; set; }

    /// <summary>Called after the JS editor is created.</summary>
    [Parameter] public EventCallback OnReady { get; set; }

    [Parameter] public Graph Graph { get; set; } = new();
    protected string ElementId => Id ?? $"df_{GetHashCode():x}";

    public double PosX { get; set; }
    public double PosY { get; set; }

    // Undo/Redo system - snapshot-based
    private readonly List<GraphSnapshot> _undoStack = new();
    private readonly List<GraphSnapshot> _redoStack = new();
    private volatile bool _isPerformingUndoRedo = false;
    private volatile bool _suppressEvents = false;
    private System.Threading.Timer? _snapshotTimer;

    // Callback to close modal (set from .razor file)
    protected Action? OnCloseModalAfterUndoRedo { get; set; }

    // Callback to attach node event handlers (set from .razor file)
    protected Action? OnAttachNodeEventHandlers { get; set; }
    private bool _pendingSnapshot = false;
    private readonly object _snapshotLock = new();
    private GraphSnapshot? _preMovementSnapshot = null; // Captures state before a drag starts

    private static JsonSerializerOptions jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _selfRef ??= DotNetObjectReference.Create(this);

            var opts = Options ?? [];
            _created = await JS.InvokeAsync<bool>("DrawflowBlazor.create", ElementId, _selfRef, opts).ConfigureAwait(false);
            if (_created)
            {
                await OnReady.InvokeAsync().ConfigureAwait(false);
            }

            // Create the wrapper:
            Editor = new BlazorWorkflow.Flow.DrawflowEditorInterop(
                callVoid: (m, a) => CallVoidAsync(m, a),
                callObject: (m, a) => JS.InvokeAsync<object?>("DrawflowBlazor.call", ElementId, m, a));

            // Clear existing graph
            await CallAsync<object>("clear").ConfigureAwait(false);

            // Import nodes to drawflow
            var json = DrawflowExporter.ExportToDrawflowJson(Graph.Nodes.Select(x => x.Value));
            await CallAsync("import", json).ConfigureAwait(false);
            await JS.InvokeVoidAsync("nextFrame").ConfigureAwait(false);

            // Re-apply port labels for nodes with multiple outputs
            foreach (var node in Graph.Nodes.Select(x => x.Value))
            {
                if (node.DeclaredOutputPorts.Count > 0)
                {
                    await JS.InvokeVoidAsync("DrawflowBlazor.labelPorts", Id, node.DrawflowNodeId, new List<List<string>>(), node.DeclaredOutputPorts.Select(x => new List<string>() { x, "" })).ConfigureAwait(false);
                }

                await JS.InvokeVoidAsync("DrawflowBlazor.setNodeWidthFromTitle", Id, node.DrawflowNodeId).ConfigureAwait(false);
            }

            // Update connection positions after initial import to prevent glitches
            await JS.InvokeVoidAsync("DrawflowBlazor.updateConnectionNodes", Id).ConfigureAwait(false);

            // Subscribe to Drawflow events to keep Graph in sync
            await OnAsync("nodeCreated").ConfigureAwait(false);
            await OnAsync("nodeMoved").ConfigureAwait(false);
            await OnAsync("connectionCreated").ConfigureAwait(false);
            await OnAsync("connectionRemoved").ConfigureAwait(false);
            await OnAsync("nodeRemoved").ConfigureAwait(false);

            // Setup keyboard listener for undo/redo
            await JS.InvokeVoidAsync("setupUndoRedoKeyboard", _selfRef, ElementId).ConfigureAwait(false);

            // Take initial snapshot after graph is fully loaded
            // This gives us a baseline state to return to
            await JS.InvokeVoidAsync("nextFrame").ConfigureAwait(false);
            TakeSnapshot();

            // Setup double-click callback
            await JS.InvokeVoidAsync(
                "window.DrawflowBlazor.setNodeDoubleClickCallback",
                ElementId,
                _selfRef
            );

            // Attach event handlers to all nodes for status updates
            // This is CRITICAL for UI feedback (pulsing animations, error states, etc.)
            AttachNodeEventHandlers();
        }
    }

    [JSInvokable]
    public async Task OnUndoRequested()
    {
        await UndoAsync().ConfigureAwait(false);
    }

    [JSInvokable]
    public async Task OnRedoRequested()
    {
        await RedoAsync().ConfigureAwait(false);
    }

    [JSInvokable]
    public async Task OnDrawflowEvent(string name, string payloadJson)
    {
        _ = Task.Run(async () =>
        {
            // Skip events fired during undo/redo restore to prevent corruption
            if (_suppressEvents) return;

            // Handle events that update the Graph
            try
            {
                switch (name)
                {
                    case "nodeCreated":
                        await HandleNodeCreated(payloadJson).ConfigureAwait(false);
                        break;

                    case "nodeMoved":
                        await HandleNodeMoved(payloadJson).ConfigureAwait(false);
                        break;

                    case "connectionCreated":
                        await HandleConnectionCreated(payloadJson).ConfigureAwait(false);
                        break;

                    case "connectionRemoved":
                        await HandleConnectionRemoved(payloadJson).ConfigureAwait(false);
                        break;

                    case "nodeRemoved":
                        await HandleNodeRemoved(payloadJson).ConfigureAwait(false);
                        break;

                    case "translate":
                        await HandleCanvasTranslate(payloadJson).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception ex)
            {
            }

            // Also invoke the user's event handler if they have one
            if (OnEvent.HasDelegate)
            {
                _ = InvokeAsync(() => OnEvent.InvokeAsync(new DrawflowEventArgs { Name = name, PayloadJson = payloadJson }));
            }
        });
    }

    private async Task HandleCanvasTranslate(string payloadJson)
    {
        // Parse: [x, y] - Drawflow passes the new canvas position
        var payload = JsonSerializer.Deserialize<JsonArray>(payloadJson, jsonSerializerOptions);
        var innerObject = payload.First().AsObject();
        innerObject.TryGetPropertyValue("x", out var jsonPosX);
        innerObject.TryGetPropertyValue("y", out var jsonPosY);

        PosX = jsonPosX.GetValue<int>();;
        PosY = jsonPosY.GetValue<int>();
    }

    private Task HandleNodeCreated(string payloadJson)
    {
        // nodeCreated events are forwarded to user event handlers via OnEvent,
        // but we don't add them to our Graph since Node instances require a BackingMethod.
        // Nodes in Graph are created programmatically with methods, not from UI interactions.
        return Task.CompletedTask;
    }

    private async Task HandleNodeMoved(string payloadJson)
    {
        // Parse: ["1"] - Drawflow passes just the node ID
        var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson, jsonSerializerOptions);

        if (payload.ValueKind != JsonValueKind.Array || payload.GetArrayLength() == 0)
            return;

        var nodeId = payload[0].ToString();

        if (Graph.Nodes.TryGetValue(nodeId, out var node) && Editor != null)
        {
            try
            {
                // Capture pre-move state before updating positions (only on first move event of a drag)
                // Use C# Graph model which still has the old position for this node
                if (_preMovementSnapshot == null && !_isPerformingUndoRedo)
                {
                    _preMovementSnapshot = GraphSnapshot.Create(Graph, PosX, PosY);
                }

                // Now update the C# node with the actual position from the editor
                var nodeData = await Editor.RawAsync<JsonElement>("getNodeFromId", nodeId).ConfigureAwait(false);
                if (nodeData.ValueKind == JsonValueKind.Object)
                {
                    if (nodeData.TryGetProperty("pos_x", out var posXProp))
                        node.PosX = posXProp.GetDouble();
                    if (nodeData.TryGetProperty("pos_y", out var posYProp))
                        node.PosY = posYProp.GetDouble();
                }

                // Schedule committing the pre-move snapshot after drag ends (debounced)
                ScheduleSnapshot();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UndoRedo] HandleNodeMoved error: {ex.Message}");
            }
        }
    }

    private Task HandleConnectionCreated(string payloadJson)
    {
        // Parse: [{"output_id":"1","input_id":"2","output_class":"output_1","input_class":"input_1"}]
        var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson, jsonSerializerOptions);

        if (payload.ValueKind != JsonValueKind.Array || payload.GetArrayLength() < 1)
            return Task.CompletedTask;

        var connectionObj = payload[0];
        if (connectionObj.ValueKind != JsonValueKind.Object)
            return Task.CompletedTask;

        if (!connectionObj.TryGetProperty("output_id", out var outputIdProp) ||
            !connectionObj.TryGetProperty("input_id", out var inputIdProp) ||
            !connectionObj.TryGetProperty("output_class", out var outputClassProp) ||
            !connectionObj.TryGetProperty("input_class", out var inputClassProp))
            return Task.CompletedTask;

        var outputId = outputIdProp.GetString();
        var inputId = inputIdProp.GetString();
        var outputClass = outputClassProp.GetString();
        var inputClass = inputClassProp.GetString();

        if (string.IsNullOrEmpty(outputId) || string.IsNullOrEmpty(inputId) ||
            string.IsNullOrEmpty(outputClass) || string.IsNullOrEmpty(inputClass))
            return Task.CompletedTask;

        if (Graph.Nodes.TryGetValue(outputId, out var sourceNode) &&
            Graph.Nodes.TryGetValue(inputId, out var targetNode))
        {
            // Take snapshot BEFORE making the change so we can undo back to this state
            TakeSnapshot();

            // Determine output port name from output_class (e.g., "output_1" -> first port)
            var outputPortName = "default";
            if (sourceNode.DeclaredOutputPorts is { Count: > 0 } ports)
            {
                // Extract port index from "output_1", "output_2", etc.
                var underscoreIndex = outputClass.LastIndexOf('_');
                if (underscoreIndex >= 0 &&
                    int.TryParse(outputClass.Substring(underscoreIndex + 1), out var portIndex) &&
                    portIndex > 0 && portIndex <= ports.Count)
                {
                    outputPortName = ports[portIndex - 1];
                }
            }

            // Add connection
            sourceNode.AddOutputConnection(outputPortName, targetNode);

            // Add to InputNodes if not already there
            if (!targetNode.InputNodes.Contains(sourceNode))
            {
                targetNode.InputNodes.Add(sourceNode);
            }

            // Auto-populate target node input with source node result if available
            if (sourceNode.Result != null)
            {
                var jsonObject = sourceNode.Result.Deserialize<JsonObject>();
                jsonObject?.SetByPath("input", jsonObject["output"]);
                jsonObject?.Remove("output");

                if (targetNode.Input is not null)
                {
                    targetNode.Input.Merge(jsonObject);
                }
                else
                {
                    targetNode.Input = jsonObject;
                }


                targetNode.Input = jsonObject;
            }
        }

        return Task.CompletedTask;
    }

    private Task HandleConnectionRemoved(string payloadJson)
    {
        // Parse: [{"output_id":"1","input_id":"2","output_class":"output_1","input_class":"input_1"}]
        var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson, jsonSerializerOptions);

        if (payload.ValueKind != JsonValueKind.Array || payload.GetArrayLength() < 1)
            return Task.CompletedTask;

        var connectionObj = payload[0];
        if (connectionObj.ValueKind != JsonValueKind.Object)
            return Task.CompletedTask;

        if (!connectionObj.TryGetProperty("output_id", out var outputIdProp) ||
            !connectionObj.TryGetProperty("input_id", out var inputIdProp) ||
            !connectionObj.TryGetProperty("output_class", out var outputClassProp) ||
            !connectionObj.TryGetProperty("input_class", out var inputClassProp))
            return Task.CompletedTask;

        var outputId = outputIdProp.GetString();
        var inputId = inputIdProp.GetString();
        var outputClass = outputClassProp.GetString();
        var inputClass = inputClassProp.GetString();

        if (string.IsNullOrEmpty(outputId) || string.IsNullOrEmpty(inputId) ||
            string.IsNullOrEmpty(outputClass) || string.IsNullOrEmpty(inputClass))
            return Task.CompletedTask;

        if (Graph.Nodes.TryGetValue(outputId, out var sourceNode) &&
            Graph.Nodes.TryGetValue(inputId, out var targetNode))
        {
            // Take snapshot BEFORE making the change so we can undo back to this state
            TakeSnapshot();

            // Determine output port name from output_class (e.g., "output_1" -> first port)
            var outputPortName = "default";
            if (sourceNode.DeclaredOutputPorts is { Count: > 0 } ports)
            {
                var underscoreIndex = outputClass.LastIndexOf('_');
                if (underscoreIndex >= 0 &&
                    int.TryParse(outputClass.Substring(underscoreIndex + 1), out var portIndex) &&
                    portIndex > 0 && portIndex <= ports.Count)
                {
                    outputPortName = ports[portIndex - 1];
                }
            }

            // Remove connection from OutputPorts
            if (sourceNode.OutputPorts.TryGetValue(outputPortName, out var targets))
            {
                targets.Remove(targetNode);
                if (targets.Count == 0)
                {
                    sourceNode.OutputPorts.Remove(outputPortName);
                }
            }

            // Remove from OutputNodes
            sourceNode.OutputNodes.Remove(targetNode);

            // Check if targetNode still has other connections from sourceNode
            bool hasOtherConnections = false;
            foreach (var portTargets in sourceNode.OutputPorts.Values)
            {
                if (portTargets.Contains(targetNode))
                {
                    hasOtherConnections = true;
                    break;
                }
            }

            // Remove from InputNodes only if no other connections exist
            if (!hasOtherConnections)
            {
                targetNode.InputNodes.Remove(sourceNode);
            }
        }

        return Task.CompletedTask;
    }

    private Task HandleNodeRemoved(string payloadJson)
    {
        // Parse: ["id"] - Drawflow passes just the node ID
        var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson, jsonSerializerOptions);

        if (payload.ValueKind != JsonValueKind.Array || payload.GetArrayLength() == 0)
            return Task.CompletedTask;

        var nodeId = payload[0].ToString();

        if (Graph.Nodes.TryGetValue(nodeId, out var removedNode))
        {
            // Take snapshot BEFORE removing the node
            TakeSnapshot();

            // Remove all connections to/from this node
            foreach (var otherNode in Graph.Nodes.Values)
            {
                if (otherNode != removedNode)
                {
                    // Remove from input connections
                    otherNode.InputNodes.Remove(removedNode);

                    // Remove from output connections
                    otherNode.OutputNodes.Remove(removedNode);

                    // Remove from OutputPorts
                    foreach (var portTargets in otherNode.OutputPorts.Values)
                    {
                        portTargets.Remove(removedNode);
                    }
                }
            }

            // Remove the node from the graph
            Graph.Nodes.TryRemove(nodeId, out _);
        }

        return Task.CompletedTask;
    }

    /// <summary>Subscribe to an event name (e.g., "nodeCreated").</summary>
    public async Task OnAsync(string eventName)
        => await JS.InvokeVoidAsync("DrawflowBlazor.on", ElementId, eventName).ConfigureAwait(false);

    /// <summary>Unsubscribe from an event name.</summary>
    public async Task OffAsync(string eventName)
        => await JS.InvokeVoidAsync("DrawflowBlazor.off", ElementId, eventName).ConfigureAwait(false);

    /// <summary>Call any Drawflow method dynamically (best-coverage path).</summary>
    public async Task<T?> CallAsync<T>(string methodName, params object[] args)
        => await JS.InvokeAsync<T?>("DrawflowBlazor.call", ElementId, methodName, args).ConfigureAwait(false);

    public async Task<object?> CallAsync(string methodName, params object[] args)
        => await JS.InvokeAsync<object?>("DrawflowBlazor.call", ElementId, methodName, args).ConfigureAwait(false);

    public BlazorWorkflow.Flow.DrawflowEditorInterop? Editor { get; set; }
    ValueTask CallVoidAsync(string method, params object?[] args)
    => JS.InvokeVoidAsync("DrawflowBlazor.call", ElementId, method, args);

    /// <summary>Get an arbitrary property on the editor.</summary>
    public async Task<T?> GetAsync<T>(string propName)
        => await JS.InvokeAsync<T?>("DrawflowBlazor.get", ElementId, propName).ConfigureAwait(false);

    /// <summary>Set an arbitrary property on the editor.</summary>
    public async Task SetAsync(string propName, object? value)
        => await JS.InvokeVoidAsync("DrawflowBlazor.set", ElementId, propName, value).ConfigureAwait(false);
    public Graph GenerateGraphV2(DrawflowGraph graph)
    {
        var concurrentNodeDict = new ConcurrentDictionary<string, Node>();

        // Ensure every node in the page gets materialized
        foreach (var (id, dfNode) in graph.Page.Data)
        {
            _ = GenerateNodeV2(graph, dfNode, concurrentNodeDict);
        }

        return new Graph
        {
            Nodes = concurrentNodeDict
        };
    }

    public Node GenerateNodeV2(
    DrawflowGraph graph,
    DrawflowNode dfNode,
    ConcurrentDictionary<string, Node>? createdNodes = null)
    {
        createdNodes ??= [];

        var nodeKey = dfNode.Id.ToString();

        // Already created? just return it
        if (createdNodes.TryGetValue(nodeKey, out var existing))
        {
            return existing;
        }

        // 1. Rehydrate internal Node from the saved data
        if (dfNode.Data is null || !dfNode.Data.TryGetValue("node", out var nodeObj))
        {
            throw new InvalidOperationException($"Drawflow node {dfNode.Id} has no 'node' payload.");
        }

        var nodeJson = nodeObj.ToString();
        var internalNode = JsonSerializer.Deserialize<Node>(nodeJson, jsonSerializerOptions)
                          ?? throw new InvalidOperationException($"Failed to deserialize internal Node for Drawflow node {dfNode.Id}.");

        // Clean up any invalid mappings for auto-handled parameters (IServiceProvider, NodeContext)
        // This handles workflows saved before these parameters were properly excluded
        var parametersToExclude = internalNode.BackingMethod.GetParameters()
            .Where(p => p.ParameterType == typeof(NodeContext) || p.ParameterType == typeof(IServiceProvider))
            .Select(p => p.Name)
            .ToHashSet();

        if (parametersToExclude.Any())
        {
            internalNode.NodeInputToMethodInputMap = internalNode.NodeInputToMethodInputMap
                .Where(m => !parametersToExclude.Contains(m.To))
                .ToList();
        }

        internalNode.DrawflowNodeId = nodeKey;
        internalNode.PosX = dfNode.PosX;
        internalNode.PosY = dfNode.PosY;

        // Put it in the map *before* recursing to handle cycles
        createdNodes[nodeKey] = internalNode;

        // 2. Build InputNodes from incoming edges
        var incomingEdges = graph.GetIncoming(nodeKey);
        var inputNodes = new List<Node>();

        foreach (var edge in incomingEdges)
        {
            var fromDfNode = graph.GetNode(edge.FromNodeId);
            var fromInternal = GenerateNodeV2(graph, fromDfNode, createdNodes);
            inputNodes.Add(fromInternal);
        }

        internalNode.InputNodes = [.. inputNodes];

        // 3. Build OutputNodes + OutputPorts from outgoing edges
        var outgoingEdges = graph.GetOutgoing(nodeKey);

        foreach (var edge in outgoingEdges)
        {
            var toDfNode = graph.GetNode(edge.ToNodeId);
            var toInternal = GenerateNodeV2(graph, toDfNode, createdNodes);

            // edge.FromOutputPort looks like "output_1", "output_2", ...
            var portName = "default";

            if (internalNode.DeclaredOutputPorts is { Count: > 0 } ports &&
                !string.IsNullOrWhiteSpace(edge.FromOutputPort))
            {
                var drawflowPortId = edge.FromOutputPort; // e.g. "output_1"
                var underscoreIndex = drawflowPortId.LastIndexOf('_');

                if (underscoreIndex >= 0 &&
                    int.TryParse(drawflowPortId[(underscoreIndex + 1)..], out var oneBasedIndex))
                {
                    var idx = oneBasedIndex - 1; // convert 1-based -> 0-based

                    if (idx >= 0 && idx < ports.Count)
                    {
                        portName = ports[idx];
                    }
                }
            }

            internalNode.AddOutputConnection(portName, toInternal);
        }

        return internalNode;
    }

    public async Task<Graph?> CreateInternalV2Graph()
    {
        if (Editor is null)
        {
            return null;
        }

        var drawflowJson = await Editor.ExportAsync().ConfigureAwait(false);
        if (drawflowJson is null)
        {
            return null;
        }

        var graph = DrawflowGraph.Parse(this, drawflowJson);
        var internalGraph = GenerateGraphV2(graph);
        return internalGraph;
    }

    // ==========================================
    // UNDO/REDO SYSTEM (Snapshot-based)
    // ==========================================

    /// <summary>
    /// Schedule committing a snapshot after a short delay (debounced).
    /// Used for operations like node moves that fire multiple events during a drag.
    /// Commits the pre-movement snapshot captured at the start of the drag.
    /// </summary>
    private void ScheduleSnapshot()
    {
        if (_isPerformingUndoRedo)
        {
            return;
        }

        lock (_snapshotLock)
        {
            _pendingSnapshot = true;

            // Dispose old timer if exists
            _snapshotTimer?.Dispose();

            // Create new timer that fires after 500ms (debounce for drag end)
            _snapshotTimer = new System.Threading.Timer(_ =>
            {
                lock (_snapshotLock)
                {
                    if (_pendingSnapshot)
                    {
                        _ = InvokeAsync(() =>
                        {
                            // Commit the pre-movement snapshot (state before drag started)
                            if (_preMovementSnapshot != null)
                            {
                                lock (_snapshotLock)
                                {
                                    _undoStack.Add(_preMovementSnapshot);
                                    _redoStack.Clear();
                                    _preMovementSnapshot = null;
                                }
                            }
                        });
                        _pendingSnapshot = false;
                    }
                }
            }, null, 500, Timeout.Infinite);
        }
    }

    /// <summary>
    /// Take an immediate snapshot of the current graph state.
    /// Uses the JS editor export when available for authoritative positions/connections.
    /// </summary>
    private void TakeSnapshot()
    {
        if (_isPerformingUndoRedo || _suppressEvents)
        {
            return;
        }

        try
        {
            if (Graph.Nodes.Count == 0)
            {
                return;
            }

            // Fall back to C# model export (positions may be slightly stale for recent moves)
            var snapshot = GraphSnapshot.Create(Graph, PosX, PosY);

            lock (_snapshotLock)
            {
                _undoStack.Add(snapshot);
                _redoStack.Clear();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UndoRedo] TakeSnapshot error: {ex.Message}");
        }
    }

    /// <summary>
    /// Take a snapshot using the JS editor's export (authoritative for positions/connections).
    /// </summary>
    private async Task TakeSnapshotFromEditorAsync()
    {
        if (_isPerformingUndoRedo || _suppressEvents || Editor == null)
        {
            return;
        }

        try
        {
            if (Graph.Nodes.Count == 0)
            {
                return;
            }

            var editorJson = await Editor.ExportAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(editorJson))
            {
                return;
            }

            // Sync C# node positions from editor before creating snapshot data
            var parsed = DrawflowGraph.Parse(this, editorJson);
            foreach (var (id, dfNode) in parsed.Page.Data)
            {
                if (Graph.Nodes.TryGetValue(id, out var node))
                {
                    node.PosX = dfNode.PosX;
                    node.PosY = dfNode.PosY;
                }
            }

            // Re-export with updated C# model (includes node data that the raw editor JSON may not have)
            var snapshot = GraphSnapshot.Create(Graph, PosX, PosY);

            lock (_snapshotLock)
            {
                _undoStack.Add(snapshot);
                _redoStack.Clear();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UndoRedo] TakeSnapshotFromEditor error: {ex.Message}");
        }
    }

    /// <summary>
    /// Undo to the last snapshot.
    /// </summary>
    public async Task UndoAsync()
    {
        GraphSnapshot previousSnapshot;
        lock (_snapshotLock)
        {
            if (_undoStack.Count == 0) return;
            previousSnapshot = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);
        }

        _isPerformingUndoRedo = true;
        _suppressEvents = true;
        try
        {
            // Capture current state from the JS editor (authoritative positions)
            var currentSnapshot = await CreateSnapshotFromEditorAsync().ConfigureAwait(false)
                                  ?? GraphSnapshot.Create(Graph, PosX, PosY);

            await RestoreSnapshotAsync(previousSnapshot).ConfigureAwait(false);

            lock (_snapshotLock)
            {
                _redoStack.Add(currentSnapshot);
            }

            // Save the restored state
            await TriggerSaveAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UndoRedo] Undo error: {ex.Message}");
        }
        finally
        {
            _suppressEvents = false;
            _isPerformingUndoRedo = false;
            _preMovementSnapshot = null;
        }
    }

    /// <summary>
    /// Redo the last undone snapshot.
    /// </summary>
    public async Task RedoAsync()
    {
        GraphSnapshot nextSnapshot;
        lock (_snapshotLock)
        {
            if (_redoStack.Count == 0) return;
            nextSnapshot = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);
        }

        _isPerformingUndoRedo = true;
        _suppressEvents = true;
        try
        {
            // Capture current state from the JS editor (authoritative positions)
            var currentSnapshot = await CreateSnapshotFromEditorAsync().ConfigureAwait(false)
                                  ?? GraphSnapshot.Create(Graph, PosX, PosY);

            await RestoreSnapshotAsync(nextSnapshot).ConfigureAwait(false);

            lock (_snapshotLock)
            {
                _undoStack.Add(currentSnapshot);
            }

            // Save the restored state
            await TriggerSaveAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UndoRedo] Redo error: {ex.Message}");
        }
        finally
        {
            _suppressEvents = false;
            _isPerformingUndoRedo = false;
            _preMovementSnapshot = null;
        }
    }

    /// <summary>
    /// Create a snapshot from the JS editor export (authoritative positions/connections).
    /// Returns null if editor is unavailable.
    /// </summary>
    private async Task<GraphSnapshot?> CreateSnapshotFromEditorAsync()
    {
        if (Editor == null) return null;

        try
        {
            var editorJson = await Editor.ExportAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(editorJson)) return null;

            // Sync C# positions from editor
            var parsed = DrawflowGraph.Parse(this, editorJson);
            foreach (var (id, dfNode) in parsed.Page.Data)
            {
                if (Graph.Nodes.TryGetValue(id, out var node))
                {
                    node.PosX = dfNode.PosX;
                    node.PosY = dfNode.PosY;
                }
            }

            return GraphSnapshot.Create(Graph, PosX, PosY);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Trigger a save via the OnEvent callback so the parent persists the current state.
    /// </summary>
    private async Task TriggerSaveAsync()
    {
        try
        {
            if (OnEvent.HasDelegate)
            {
                await InvokeAsync(() => OnEvent.InvokeAsync(new DrawflowEventArgs
                {
                    Name = "undoRedo",
                    PayloadJson = "[]"
                }));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UndoRedo] TriggerSave error: {ex.Message}");
        }
    }

    /// <summary>
    /// Restore graph state from a snapshot.
    /// Events are suppressed by the caller (_suppressEvents = true).
    /// Repopulates the existing Graph object so the parent's reference stays valid.
    /// </summary>
    private async Task RestoreSnapshotAsync(GraphSnapshot snapshot)
    {
        if (Editor == null)
        {
            throw new InvalidOperationException("Editor is not initialized");
        }

        // Close any open modal BEFORE restoring to prevent ghost node issues
        OnCloseModalAfterUndoRedo?.Invoke();

        // Clear the Drawflow editor
        await Editor.ClearAsync().ConfigureAwait(false);

        // Wait a frame so clear-related events drain while still suppressed
        await JS.InvokeVoidAsync("nextFrame").ConfigureAwait(false);

        // Import the Drawflow JSON from the snapshot
        await Editor.ImportAsync(snapshot.DrawflowJson).ConfigureAwait(false);

        // Wait for DOM to settle after import
        await JS.InvokeVoidAsync("nextFrame").ConfigureAwait(false);

        // Parse the Drawflow JSON to regenerate the Graph object
        var drawflowGraph = DrawflowGraph.Parse(this, snapshot.DrawflowJson);
        var newGraph = GenerateGraphV2(drawflowGraph);

        // Repopulate the existing Graph.Nodes so the parent's reference stays valid
        Graph.Nodes.Clear();
        foreach (var kvp in newGraph.Nodes)
        {
            Graph.Nodes[kvp.Key] = kvp.Value;
        }

        // Restore canvas position
        PosX = snapshot.CanvasPosX;
        PosY = snapshot.CanvasPosY;

        // Re-apply port labels for nodes with multiple outputs
        foreach (var node in Graph.Nodes.Select(x => x.Value))
        {
            if (node.DeclaredOutputPorts.Count > 0)
            {
                await JS.InvokeVoidAsync("DrawflowBlazor.labelPorts", Id, node.DrawflowNodeId,
                    new List<List<string>>(),
                    node.DeclaredOutputPorts.Select(x => new List<string>() { x, "" })).ConfigureAwait(false);
            }

            await JS.InvokeVoidAsync("DrawflowBlazor.setNodeWidthFromTitle", Id, node.DrawflowNodeId).ConfigureAwait(false);
        }

        // Update connection positions
        await JS.InvokeVoidAsync("DrawflowBlazor.updateConnectionNodes", Id).ConfigureAwait(false);

        // Reattach event handlers to all nodes
        OnAttachNodeEventHandlers?.Invoke();
    }

    /// <summary>
    /// Clear undo/redo history.
    /// </summary>
    public void ClearHistory()
    {
        lock (_snapshotLock)
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
        _snapshotTimer?.Dispose();
        _snapshotTimer = null;
        _pendingSnapshot = false;
        _preMovementSnapshot = null;
    }

    /// <summary>Destroy the editor instance.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            // Dispose snapshot timer
            _snapshotTimer?.Dispose();

            if (JS is not null && _created)
            {
                 await JS.InvokeVoidAsync("DrawflowBlazor.destroy", ElementId).ConfigureAwait(false);
                 await JS.InvokeVoidAsync("removeUndoRedoKeyboard", ElementId).ConfigureAwait(false);
            }
        }
        catch { /* ignore */ }
        finally
        {
            _selfRef?.Dispose();
        }
    }
}
