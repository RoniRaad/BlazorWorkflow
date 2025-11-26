using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models;
using BlazorExecutionFlow.Models.DTOs;
using BlazorExecutionFlow.Models.NodeV2;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorExecutionFlow.Components;

public class DrawflowEventArgs : EventArgs
{
    public required string Name { get; init; }
    /// <summary>Raw JSON payload array from Drawflow event arguments.</summary>
    public required string PayloadJson { get; init; }

    /// <summary>Deserialize the payload as T.</summary>
    public T? GetPayload<T>() => JsonSerializer.Deserialize<T>(PayloadJson);
}

public partial class BlazorExecutionFlowGraphBase : ComponentBase, IAsyncDisposable
{
    private DotNetObjectReference<BlazorExecutionFlowGraphBase>? _selfRef;
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
    private readonly Stack<GraphSnapshot> _undoStack = new();
    private readonly Stack<GraphSnapshot> _redoStack = new();
    private bool _isPerformingUndoRedo = false;
    private System.Threading.Timer? _snapshotTimer;

    // Callback to close modal (set from .razor file)
    protected Action? OnCloseModalAfterUndoRedo { get; set; }

    // Callback to attach node event handlers (set from .razor file)
    protected Action? OnAttachNodeEventHandlers { get; set; }
    private bool _pendingSnapshot = false;
    private readonly object _snapshotLock = new();
    private string? _lastSnapshotJson = null; // Track last snapshot to avoid duplicates

    private static JsonSerializerOptions jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _selfRef ??= DotNetObjectReference.Create(this);

            var opts = Options ?? [];
            _created = await JS.InvokeAsync<bool>("DrawflowBlazor.create", ElementId, _selfRef, opts);
            if (_created)
            {
                await OnReady.InvokeAsync();
            }

            // Create the wrapper:
            Editor = new BlazorExecutionFlow.Flow.DrawflowEditorInterop(
                callVoid: (m, a) => CallVoidAsync(m, a),
                callObject: (m, a) => JS.InvokeAsync<object?>("DrawflowBlazor.call", ElementId, m, a));

            // Clear existing graph
            await CallAsync<object>("clear");

            // Import nodes to drawflow
            var json = DrawflowExporter.ExportToDrawflowJson(Graph.Nodes.Select(x => x.Value));
            await CallAsync("import", json);
            await JS.InvokeVoidAsync("nextFrame");

            // Re-apply port labels for nodes with multiple outputs
            foreach (var node in Graph.Nodes.Select(x => x.Value))
            {
                if (node.DeclaredOutputPorts.Count > 0)
                {
                    await JS.InvokeVoidAsync("DrawflowBlazor.labelPorts", Id, node.DrawflowNodeId, new List<List<string>>(), node.DeclaredOutputPorts.Select(x => new List<string>() { x, "" }));
                }
            }

            // Update connection positions after initial import to prevent glitches
            await JS.InvokeVoidAsync("DrawflowBlazor.updateConnectionNodes", Id);

            // Subscribe to Drawflow events to keep Graph in sync
            await OnAsync("nodeCreated");
            await OnAsync("nodeMoved");
            await OnAsync("connectionCreated");
            await OnAsync("connectionRemoved");
            await OnAsync("nodeRemoved");

            // Setup keyboard listener for undo/redo
            await JS.InvokeVoidAsync("setupUndoRedoKeyboard", _selfRef, ElementId);

            // Take initial snapshot after graph is fully loaded
            // This gives us a baseline state to return to
            await Task.Delay(100); // Small delay to ensure everything is settled
            TakeSnapshot();
        }
    }

    [JSInvokable]
    public async Task OnUndoRequested()
    {
        Console.WriteLine($"[Undo] Undo requested. Stack size: {_undoStack.Count}");
        await UndoAsync();
    }

    [JSInvokable]
    public async Task OnRedoRequested()
    {
        Console.WriteLine($"[Redo] Redo requested. Stack size: {_redoStack.Count}");
        await RedoAsync();
    }

    [JSInvokable]
    public async Task OnDrawflowEvent(string name, string payloadJson)
    {
        _ = Task.Run(async () =>
        {
            // Handle events that update the Graph
            try
            {
                switch (name)
                {
                    case "nodeCreated":
                        await HandleNodeCreated(payloadJson);
                        break;

                    case "nodeMoved":
                        await HandleNodeMoved(payloadJson);
                        break;

                    case "connectionCreated":
                        await HandleConnectionCreated(payloadJson);
                        break;

                    case "connectionRemoved":
                        await HandleConnectionRemoved(payloadJson);
                        break;

                    case "nodeRemoved":
                        await HandleNodeRemoved(payloadJson);
                        break;

                    case "translate":
                        await HandleCanvasTranslate(payloadJson);
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log but don't crash - just let the event through
                Console.WriteLine($"Error handling Drawflow event '{name}': {ex.Message}");
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

        // We need to query the editor for the actual position since Drawflow doesn't pass it in the event
        if (Graph.Nodes.TryGetValue(nodeId, out var node) && Editor != null)
        {
            try
            {
                var nodeData = await Editor.RawAsync<JsonElement>("getNodeFromId", nodeId);
                if (nodeData.ValueKind == JsonValueKind.Object)
                {
                    if (nodeData.TryGetProperty("pos_x", out var posXProp))
                        node.PosX = posXProp.GetDouble();
                    if (nodeData.TryGetProperty("pos_y", out var posYProp))
                        node.PosY = posYProp.GetDouble();
                }

                // Schedule a snapshot after a short delay (debounced)
                ScheduleSnapshot();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting node position: {ex.Message}");
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
        => await JS.InvokeVoidAsync("DrawflowBlazor.on", ElementId, eventName);

    /// <summary>Unsubscribe from an event name.</summary>
    public async Task OffAsync(string eventName)
        => await JS.InvokeVoidAsync("DrawflowBlazor.off", ElementId, eventName);

    /// <summary>Call any Drawflow method dynamically (best-coverage path).</summary>
    public async Task<T?> CallAsync<T>(string methodName, params object[] args)
        => await JS.InvokeAsync<T?>("DrawflowBlazor.call", ElementId, methodName, args);

    public async Task<object?> CallAsync(string methodName, params object[] args)
        => await JS.InvokeAsync<object?>("DrawflowBlazor.call", ElementId, methodName, args);

    public BlazorExecutionFlow.Flow.DrawflowEditorInterop? Editor { get; set; }
    ValueTask CallVoidAsync(string method, params object?[] args)
    => JS.InvokeVoidAsync("DrawflowBlazor.call", ElementId, method, args);

    /// <summary>Get an arbitrary property on the editor.</summary>
    public async Task<T?> GetAsync<T>(string propName)
        => await JS.InvokeAsync<T?>("DrawflowBlazor.get", ElementId, propName);

    /// <summary>Set an arbitrary property on the editor.</summary>
    public async Task SetAsync(string propName, object? value)
        => await JS.InvokeVoidAsync("DrawflowBlazor.set", ElementId, propName, value);
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
    /// Schedule a snapshot after a short delay (debounced).
    /// Used for operations like node moves that fire multiple events.
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

            // Create new timer that fires after 500ms (increased for better debouncing)
            _snapshotTimer = new System.Threading.Timer(_ =>
            {
                lock (_snapshotLock)
                {
                    if (_pendingSnapshot)
                    {
                        // Use InvokeAsync to ensure we're on the UI thread
                        _ = InvokeAsync(() =>
                        {
                            TakeSnapshot();
                        });
                        _pendingSnapshot = false;
                    }
                }
            }, null, 500, Timeout.Infinite);
        }
    }

    /// <summary>
    /// Take an immediate snapshot of the current graph state.
    /// </summary>
    private void TakeSnapshot()
    {
        if (_isPerformingUndoRedo)
        {
            return;
        }

        try
        {
            // Ensure we have nodes to snapshot
            if (Graph.Nodes.Count == 0)
            {
                Console.WriteLine("[Snapshot] Skipping snapshot - graph is empty");
                return;
            }

            var snapshot = GraphSnapshot.Create(Graph, PosX, PosY);

            // Avoid taking duplicate snapshots (same state captured twice)
            if (_lastSnapshotJson != null && _lastSnapshotJson == snapshot.DrawflowJson)
            {
                Console.WriteLine("[Snapshot] Skipping duplicate snapshot");
                return;
            }

            _lastSnapshotJson = snapshot.DrawflowJson;
            _undoStack.Push(snapshot);
            _redoStack.Clear(); // Clear redo stack when a new snapshot is taken
            Console.WriteLine($"[Snapshot] Captured graph state with {Graph.Nodes.Count} nodes. Undo stack size: {_undoStack.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Snapshot] Error taking snapshot: {ex.Message}");
            Console.WriteLine($"[Snapshot] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Undo to the last snapshot.
    /// </summary>
    public async Task UndoAsync()
    {
        if (_undoStack.Count == 0)
        {
            Console.WriteLine("[Undo] Nothing to undo");
            return;
        }

        _isPerformingUndoRedo = true;
        try
        {
            // Save current state to redo stack
            var currentSnapshot = GraphSnapshot.Create(Graph, PosX, PosY);

            // Pop and restore previous state
            var previousSnapshot = _undoStack.Pop();
            await RestoreSnapshotAsync(previousSnapshot);

            // Push current state to redo stack
            _redoStack.Push(currentSnapshot);

            Console.WriteLine($"[Undo] Restored previous state. Undo stack size: {_undoStack.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Undo] Error during undo: {ex.Message}");
        }
        finally
        {
            _isPerformingUndoRedo = false;
        }
    }

    /// <summary>
    /// Redo the last undone snapshot.
    /// </summary>
    public async Task RedoAsync()
    {
        if (_redoStack.Count == 0)
        {
            Console.WriteLine("[Redo] Nothing to redo");
            return;
        }

        _isPerformingUndoRedo = true;
        try
        {
            // Save current state to undo stack
            var currentSnapshot = GraphSnapshot.Create(Graph, PosX, PosY);

            // Pop and restore next state
            var nextSnapshot = _redoStack.Pop();
            await RestoreSnapshotAsync(nextSnapshot);

            // Push current state to undo stack
            _undoStack.Push(currentSnapshot);

            Console.WriteLine($"[Redo] Restored next state. Redo stack size: {_redoStack.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Redo] Error during redo: {ex.Message}");
        }
        finally
        {
            _isPerformingUndoRedo = false;
        }
    }

    /// <summary>
    /// Restore graph state from a snapshot.
    /// </summary>
    private async Task RestoreSnapshotAsync(GraphSnapshot snapshot)
    {
        if (Editor == null)
        {
            throw new InvalidOperationException("Editor is not initialized");
        }

        try
        {
            Console.WriteLine($"[RestoreSnapshot] Starting restore...");

            // Clear the Drawflow editor
            await Editor.ClearAsync();

            // Import the Drawflow JSON from the snapshot
            await Editor.ImportAsync(snapshot.DrawflowJson);

            // Wait for next frame to ensure DOM is ready
            await JS.InvokeVoidAsync("nextFrame");

            // Parse the Drawflow JSON to regenerate the Graph object
            var drawflowGraph = DrawflowGraph.Parse(this, snapshot.DrawflowJson);
            var newGraph = GenerateGraphV2(drawflowGraph);

            Console.WriteLine($"[RestoreSnapshot] Old Graph had {Graph.Nodes.Count} nodes");
            Console.WriteLine($"[RestoreSnapshot] New Graph has {newGraph.Nodes.Count} nodes");
            Console.WriteLine($"[RestoreSnapshot] New Graph node IDs: {string.Join(", ", newGraph.Nodes.Keys)}");

            Graph = newGraph;

            // Restore canvas position
            PosX = snapshot.CanvasPosX;
            PosY = snapshot.CanvasPosY;

            // Update the last snapshot to match what we just restored
            // This prevents the restore from triggering a new snapshot
            _lastSnapshotJson = snapshot.DrawflowJson;

            // Re-apply port labels for nodes with multiple outputs
            foreach (var node in Graph.Nodes.Select(x => x.Value))
            {
                if (node.DeclaredOutputPorts.Count > 0)
                {
                    await JS.InvokeVoidAsync("DrawflowBlazor.labelPorts", Id, node.DrawflowNodeId,
                        new List<List<string>>(),
                        node.DeclaredOutputPorts.Select(x => new List<string>() { x, "" }));
                }

                await JS.InvokeVoidAsync("DrawflowBlazor.setNodeWidthFromTitle", Id, node.DrawflowNodeId);
            }

            // Update connection positions
            await JS.InvokeVoidAsync("DrawflowBlazor.updateConnectionNodes", Id);

            // Reattach event handlers to all nodes
            // This is CRITICAL - without it, nodes won't respond to interactions
            OnAttachNodeEventHandlers?.Invoke();
            Console.WriteLine($"[RestoreSnapshot] Reattached event handlers to {Graph.Nodes.Count} nodes");

            //Close any open modal to prevent "ghost node" issues
            OnCloseModalAfterUndoRedo?.Invoke();

            Console.WriteLine($"[RestoreSnapshot] Successfully restored graph with {Graph.Nodes.Count} nodes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RestoreSnapshot] Error restoring snapshot: {ex.Message}");
            Console.WriteLine($"[RestoreSnapshot] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Clear undo/redo history.
    /// </summary>
    public void ClearHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _snapshotTimer?.Dispose();
        _snapshotTimer = null;
        _pendingSnapshot = false;
        _lastSnapshotJson = null;
        Console.WriteLine("[ClearHistory] Cleared all undo/redo history");
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
                 await JS.InvokeVoidAsync("DrawflowBlazor.destroy", ElementId);
                 await JS.InvokeVoidAsync("removeUndoRedoKeyboard", ElementId);
            }
        }
        catch { /* ignore */ }
        finally
        {
            _selfRef?.Dispose();
        }
    }
}
