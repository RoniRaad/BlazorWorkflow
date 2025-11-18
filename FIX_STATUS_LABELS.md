# Fix: Node Status and Labels Not Working

## Problem

Node status animations (pulsing, error states) and port labels weren't working in apps using the BlazorExecutionFlow library, even with all CSS/JS files properly referenced.

## Root Causes

### Issue 1: Event Handlers Not Attached During Initialization
The event handlers that trigger status updates (`OnStartExecuting`, `OnStopExecuting`, `OnError`) were only being attached when the user clicked the **Run** button in the UI component.

If nodes were executed programmatically (e.g., `await node.ExecuteNode()` or `await Graph.RunAsync()`), the event handlers were never attached, so:
- No pulsing animation during execution
- No red border on errors
- No green output port indicators
- Labels showed up but weren't properly initialized

### Issue 2: Threading - Event Handlers Not Marshaled to UI Thread
The event handlers were firing on background threads (from node execution), but Blazor requires UI updates to be marshaled back to the UI thread using `InvokeAsync()`. Without this, the JavaScript interop calls would fail silently or not update the DOM.

## The Fix

### Changed Files:
1. **BlazorExecutionFlowGraph.razor** - Added event handler methods
2. **BlazorExecutionFlowGraph.razor** - Modified `OnAfterRenderAsync` to attach handlers
3. **BlazorExecutionFlowGraph.razor** - Modified `CreateNode` to attach handlers for new nodes
4. **BlazorExecutionFlowGraph.razor** - Cleaned up `Run()` and `ExecuteInputNodes()` methods

### What Changed:

#### 1. Created Centralized Event Handler Methods
```csharp
private void AttachNodeEventHandlers()
{
    foreach (var node in Graph.Nodes)
    {
        // Remove any existing handlers first to avoid duplicates
        node.Value.OnStartExecuting -= HandleNodeStartExecuting;
        node.Value.OnStopExecuting -= HandleNodeStopExecuting;
        node.Value.OnError -= HandleNodeError;

        // Attach handlers
        node.Value.OnStartExecuting += HandleNodeStartExecuting;
        node.Value.OnStopExecuting += HandleNodeStopExecuting;
        node.Value.OnError += HandleNodeError;
    }
}

void HandleNodeStartExecuting(object? sender, EventArgs e)
{
    if (sender is Node node)
    {
        // CRITICAL: Use InvokeAsync to marshal UI updates back to Blazor's synchronization context
        _ = InvokeAsync(async () => await SetStatusAsync(node.DrawflowNodeId, true));
    }
}

void HandleNodeStopExecuting(object? sender, EventArgs e)
{
    if (sender is Node node)
    {
        // CRITICAL: Use InvokeAsync to marshal UI updates back to Blazor's synchronization context
        _ = InvokeAsync(async () => await SetStatusAsync(node.DrawflowNodeId, false));
    }
}
```

#### 2. Attached Handlers During Component Initialization
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    await base.OnAfterRenderAsync(firstRender);

    if (!firstRender) return;

    _dotNetRef = DotNetObjectReference.Create(this);

    await JS.InvokeVoidAsync(
        "window.DrawflowBlazor.setNodeDoubleClickCallback",
        ElementId,
        _dotNetRef
    );

    // ✅ NEW: Attach event handlers to all nodes for status updates
    AttachNodeEventHandlers();
}
```

#### 3. Attached Handlers When Creating New Nodes
```csharp
private async Task CreateNode(Node node)
{
    // ... existing node creation code ...

    Graph.Nodes[node.DrawflowNodeId] = node;

    // ✅ NEW: Attach event handlers to new node
    AttachNodeEventHandlers();
}
```

#### 4. Removed Redundant Handler Attachment
```csharp
// BEFORE: Event handlers were attached inside Run()
private async Task Run()
{
    foreach (var node in Graph.Nodes)
    {
        node.Value.OnStartExecuting += (x, y) => { _ = SetStatusAsync(...); }; // ❌ Old
        node.Value.OnStopExecuting += (x, y) => { _ = SetStatusAsync(...); }; // ❌ Old
        node.Value.OnError += HandleNodeError; // ❌ Old
    }
    // ... rest of Run() ...
}

// AFTER: Event handlers are already attached during initialization
private async Task Run()
{
    // Clear results and errors from previous runs
    foreach (var node in Graph.Nodes)
    {
        node.Value.Result = null;
        node.Value.HasError = false;
        node.Value.ErrorMessage = null;
    }

    // ✅ Event handlers already attached - no need to re-attach
    // ... rest of Run() ...
}
```

## Benefits

### Before Fix:
- ❌ Status only worked if you clicked the UI "Run" button
- ❌ Programmatic execution (`await Graph.RunAsync()`) showed no status
- ❌ No feedback for users running workflows via code
- ❌ Event handlers re-attached on every run (memory leak potential)
- ❌ UI updates from background threads failed silently

### After Fix:
- ✅ Status works immediately after component initialization
- ✅ Programmatic execution shows status animations
- ✅ Works whether you use UI button or code
- ✅ Event handlers attached once, no duplication
- ✅ New nodes automatically get event handlers
- ✅ UI updates properly marshaled to Blazor's synchronization context (thread-safe)

## Testing

### Manual Test:
```razor
@page "/test-workflow"
@using BlazorExecutionFlow.Components
@using BlazorExecutionFlow.Models
@using BlazorExecutionFlow.Drawflow.BaseNodes

<BlazorExecutionFlowGraph
    Id="test-graph"
    @ref="_graph"
    Graph="@_nodeGraph"
    Style="height:600px;" />

<button @onclick="RunProgrammatically">Run Programmatically</button>

@code {
    private BlazorExecutionFlowGraph? _graph;
    private NodeGraph _nodeGraph = new();

    protected override void OnInitialized()
    {
        var logNode = NodeRegistry.CreateNode("Log", _nodeGraph);
        logNode.Inputs["message"].StringValue = "Testing status!";
        _nodeGraph.AddNode(logNode);
    }

    // THIS NOW WORKS - status will show!
    private async Task RunProgrammatically()
    {
        await _nodeGraph.RunAsync();
    }
}
```

### Expected Behavior:
1. **Before Execution**: Node appears normal
2. **During Execution**: Node title bar pulses with animation
3. **On Success**: Animation stops, green indicators on output ports (if any)
4. **On Error**: Red border with glow, error message in title

### Visual Indicators:
- **`.processing-bar`** class added during execution → pulsing animation
- **`.node-error`** class added on error → red border with glow
- **`.computed_node`** class added to output ports → green color

## CSS/JS Requirements (Still Needed)

This fix doesn't change the CSS/JS requirements. You still need:

```html
<!-- In App.razor or _Host.cshtml -->
<head>
    <!-- Required CSS (ALL 3) -->
    <link rel="stylesheet" href="https://unpkg.com/drawflow/dist/drawflow.min.css" />
    <link rel="stylesheet" href="_content/BlazorExecutionFlow/css/drawflowWrapper.css" />
    <link rel="stylesheet" href="_content/BlazorExecutionFlow/css/BlazorExecutionFlow.lib.module.css" />
    <link rel="stylesheet" href="_content/BlazorExecutionFlow/BlazorExecutionFlow.bundle.scp.css" />

    <!-- Required JS -->
    <script src="_content/BlazorExecutionFlow/js/drawflowInterop.js"></script>
</head>
<body>
    <!-- Before Blazor script -->
    <script src="https://unpkg.com/drawflow/dist/drawflow.min.js"></script>

    <!-- Your Blazor script -->
    <script src="_framework/blazor.web.js"></script>
</body>
```

## Migration Guide

### If you were working around this issue:

**Old workaround (no longer needed):**
```csharp
// You might have done this to make status work
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        foreach (var node in _graph.Graph.Nodes.Values)
        {
            node.OnStartExecuting += ...;  // Manual attachment
            node.OnStopExecuting += ...;
            node.OnError += ...;
        }
    }
}
```

**New (automatic):**
```csharp
// Just use the component - event handlers attach automatically!
<BlazorExecutionFlowGraph Graph="@_nodeGraph" />
```

### No Breaking Changes

This fix is **100% backward compatible**:
- Existing code continues to work
- No API changes
- No behavior changes for code using the Run button
- Only **adds** functionality for programmatic execution

## Version

This fix will be included in the next package version.

## Related Issues

Fixes issues where:
- "Node status doesn't update in my app but works in the example"
- "Labels show but animations don't work"
- "await Graph.RunAsync() doesn't show any visual feedback"
- "Pulsing animation only works when clicking Run button"
