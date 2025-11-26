using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models.NodeV2;

namespace BlazorExecutionFlow.Models
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
        /// Create a snapshot from the current graph state.
        /// </summary>
        public static GraphSnapshot Create(Graph graph, double posX, double posY)
        {
            // Export to Drawflow JSON which includes connections
            var drawflowJson = DrawflowExporter.ExportToDrawflowJson(graph.Nodes.Select(x => x.Value));

            return new GraphSnapshot
            {
                DrawflowJson = drawflowJson,
                CanvasPosX = posX,
                CanvasPosY = posY
            };
        }
    }
}
