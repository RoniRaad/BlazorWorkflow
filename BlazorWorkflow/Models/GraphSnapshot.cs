using BlazorWorkflow.Helpers;
using BlazorWorkflow.Models.NodeV2;

namespace BlazorWorkflow.Models
{
    /// <summary>
    /// Represents a complete snapshot of the graph state for undo/redo.
    /// Stores the Drawflow JSON representation to preserve all connections and node states.
    /// </summary>
    public class GraphSnapshot
    {
        /// <summary>
        /// The Drawflow JSON representation of the graph.
        /// This includes all nodes, connections, and positions.
        /// </summary>
        public string DrawflowJson { get; set; } = string.Empty;

        /// <summary>
        /// Canvas X position for viewport restoration.
        /// </summary>
        public double CanvasPosX { get; set; }

        /// <summary>
        /// Canvas Y position for viewport restoration.
        /// </summary>
        public double CanvasPosY { get; set; }

        /// <summary>
        /// Create a snapshot from the current graph state using the C# model.
        /// Prefer CreateFromEditor when the JS editor is available for authoritative positions.
        /// </summary>
        public static GraphSnapshot Create(Graph graph, double posX, double posY)
        {
            var drawflowJson = DrawflowExporter.ExportToDrawflowJson(graph.Nodes.Select(x => x.Value));

            return new GraphSnapshot
            {
                DrawflowJson = drawflowJson,
                CanvasPosX = posX,
                CanvasPosY = posY
            };
        }

        /// <summary>
        /// Create a snapshot from the JS editor's export (authoritative for positions/connections).
        /// </summary>
        public static GraphSnapshot CreateFromEditorJson(string drawflowJson, double posX, double posY)
        {
            return new GraphSnapshot
            {
                DrawflowJson = drawflowJson,
                CanvasPosX = posX,
                CanvasPosY = posY
            };
        }
    }
}
