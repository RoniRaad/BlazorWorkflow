using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BlazorExecutionFlow.Models.NodeV2;

namespace BlazorExecutionFlow.Helpers
{
    /// <summary>
    /// Helper class for discovering workflow inputs from GetInput nodes in a workflow.
    /// </summary>
    public static class WorkflowInputDiscovery
    {
        /// <summary>
        /// Scans a workflow's serialized flow data and extracts all input names
        /// requested by GetInput nodes.
        /// </summary>
        /// <param name="flowDataJson">The serialized workflow JSON (FlowData property from WorkflowInfo)</param>
        /// <returns>List of unique input names discovered in the workflow</returns>
        public static List<string> DiscoverInputs(Graph flowGraph)
        {
            var inputNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (flowGraph.Nodes.IsEmpty)
                return [];

            try
            {
                foreach(var node in flowGraph.Nodes.Values)
                {
                    if (node.BackingMethod?.Name == "GetInput")
                    {
                        var inputNameMapping = node.NodeInputToMethodInputMap
                            .FirstOrDefault(m => m.To == "inputName");
                        if (inputNameMapping != null && !string.IsNullOrWhiteSpace(inputNameMapping.From))
                        {
                            // Clean up the value - remove quotes and whitespace
                            var inputValue = inputNameMapping.From.Trim().Trim('"');
                            if (!string.IsNullOrWhiteSpace(inputValue))
                            {
                                inputNames.Add(inputValue);
                            }
                        }
                    }
                }

                return [.. inputNames.OrderBy(x => x)];
            }
            catch
            {
                // Invalid JSON, return empty list
                return new List<string>();
            }
        }

        private static void ExtractInputNamesFromNode(JsonElement nodeElement, HashSet<string> inputNames)
        {
            // Check if this node has a BackingMethod
            if (!nodeElement.TryGetProperty("BackingMethod", out var backingMethodElement))
                return;

            // Check if the method name is "GetInput"
            if (backingMethodElement.TryGetProperty("Name", out var nameElement) &&
                nameElement.GetString() == "GetInput")
            {
                // Look for the input name in the NodeInputToMethodInputMap
                if (nodeElement.TryGetProperty("NodeInputToMethodInputMap", out var mappingsElement) &&
                    mappingsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var mapping in mappingsElement.EnumerateArray())
                    {
                        // Look for mapping where To = "inputName" (the parameter name)
                        if (mapping.TryGetProperty("To", out var toElement) &&
                            toElement.GetString() == "inputName" &&
                            mapping.TryGetProperty("From", out var fromElement))
                        {
                            var inputValue = fromElement.GetString();
                            if (!string.IsNullOrWhiteSpace(inputValue))
                            {
                                // Clean up the value - remove quotes and whitespace
                                inputValue = inputValue.Trim().Trim('"');

                                if (!string.IsNullOrWhiteSpace(inputValue))
                                {
                                    inputNames.Add(inputValue);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Discovers inputs from a deserialized list of nodes.
        /// </summary>
        public static List<string> DiscoverInputsFromNodes(IEnumerable<Node> nodes)
        {
            var inputNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in nodes)
            {
                if (node.BackingMethod?.Name == "GetInput")
                {
                    // Look for the inputName parameter mapping
                    var inputNameMapping = node.NodeInputToMethodInputMap
                        .FirstOrDefault(m => m.To == "inputName");

                    if (inputNameMapping != null && !string.IsNullOrWhiteSpace(inputNameMapping.From))
                    {
                        // Clean up the value - remove quotes and whitespace
                        var inputValue = inputNameMapping.From.Trim().Trim('"');

                        if (!string.IsNullOrWhiteSpace(inputValue))
                        {
                            inputNames.Add(inputValue);
                        }
                    }
                }
            }

            return inputNames.OrderBy(x => x).ToList();
        }
    }
}
