using System.Text.Json;
using BlazorExecutionFlow.Models.DTOs;

namespace BlazorExecutionFlow.Flow;

public sealed partial class DrawflowEditorInterop
{
    // Non-generic delegates we store:
    private readonly Func<string, object?[], ValueTask<object?>> _callObject;
    private readonly Func<string, object?[], ValueTask> _callVoid;

    public DrawflowEditorInterop(
        Func<string, object?[], ValueTask> callVoid,
        Func<string, object?[], ValueTask<object?>> callObject)
    {
        _callVoid = callVoid;
        _callObject = callObject;
    }

    // Generic helper used by all typed wrappers:
    private async ValueTask<T?> CallAsync<T>(string method, params object?[] args)
    {
        var result = await _callObject(method, args);
        if (result is null) return default;

        // If JS already gave us the right .NET type:
        if (result is T ok) return ok;

        // If it came back as JsonElement/JSON, try to convert to T:
        if (result is JsonElement je)
        {
            // Special case: if T is string and we have a JsonElement, return the raw JSON text
            if (typeof(T) == typeof(string))
            {
                return (T)(object)je.GetRawText();
            }
            try { return je.Deserialize<T>(); } catch { return default; }
        }

        // Last-chance direct cast (e.g., JS number -> boxed double -> T):
        try { return (T)Convert.ChangeType(result, typeof(T)); }
        catch { return default; }
    }
    public enum EditorMode { Edit, Fixed, View }
    private static string ModeToString(EditorMode m) => m switch
    {
        EditorMode.Edit => "edit",
        EditorMode.Fixed => "fixed",
        EditorMode.View => "view",
        _ => "edit"
    };

    public ValueTask ExportRawAsync() => _callVoid("export", Array.Empty<object?>()); // if you just need side-effects
    public ValueTask<string?> ExportAsync() => CallAsync<string?>("export");

    public ValueTask ImportAsync(object drawflowJson)
        => _callVoid("import", new[] { drawflowJson });

    public ValueTask ClearAsync()
        => _callVoid("clearModuleSelected", Array.Empty<object?>());

    public ValueTask<string?> GetModuleAsync()
        => CallAsync<string?>("getModuleSelected");

    public ValueTask SetModuleAsync(string moduleName)
        => _callVoid("changeModule", new object?[] { moduleName });

    public ValueTask SetModeAsync(EditorMode mode)
        => _callVoid("setMode", new object?[] { ModeToString(mode) });

    public ValueTask<int?> AddNodeAsync(
        string name, int inputs, int outputs, double x, double y,
        string? cssClass, object? data, string? html, bool typeNode = false)
        => CallAsync<int?>("addNode", name, inputs, outputs, x, y, cssClass, data, html, typeNode);

    public ValueTask RemoveNodeAsync(int nodeId)
        => _callVoid("removeNodeId", new object?[] { nodeId });

    public ValueTask MoveNodeAsync(int nodeId, int x, int y)
        => _callVoid("updateNodePosition", new object?[] { nodeId, x, y });

    public ValueTask<DfNode?> GetNodeAsync(int nodeId)
        => CallAsync<DfNode?>("getNodeFromId", nodeId);

    public ValueTask UpdateNodeDataAsync(int nodeId, object? data)
        => _callVoid("updateNodeDataFromId", new object?[] { nodeId, data });

    public ValueTask AddConnectionAsync(string idOutput, string idInput, string outputClass, string inputClass)
        => _callVoid("addConnection", new object?[] { idOutput, idInput, outputClass, inputClass });

    public ValueTask RemoveConnectionAsync(int idOutput, int idInput, string outputClass, string inputClass)
    => _callVoid("removeSingleConnection", new object?[] { idOutput, idInput, outputClass, inputClass });

    public ValueTask UndoAsync() => _callVoid("undo", Array.Empty<object?>());
    public ValueTask RedoAsync() => _callVoid("redo", Array.Empty<object?>());

    // Escape hatches
    public ValueTask<T?> RawAsync<T>(string method, params object?[] args) => CallAsync<T>(method, args);
    public ValueTask RawVoidAsync(string method, params object?[] args) => _callVoid(method, args);
}
