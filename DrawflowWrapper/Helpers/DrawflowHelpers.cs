using System.Reflection;
using DrawflowWrapper.Components;
using DrawflowWrapper.Drawflow.Attributes;
using DrawflowWrapper.Drawflow.BaseNodes;
using DrawflowWrapper.Models;
using DrawflowWrapper.Models.NodeV2;
using Microsoft.JSInterop;
using NJsonSchema;

namespace DrawflowWrapper.Helpers
{
    public static class DrawflowHelpers
    {
        public class NodeStatus
        {
            public bool IsRunning { get; set; }
            public Dictionary<int, object> OutputPortResults { get; set; } = [];
        }

        public static List<Node> GetNodesObjectsV2()
        {
            var nodes = new List<Node>();
            Type type = typeof(BaseNodeCollection);

            var methodsWithAttr = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetCustomAttributes(typeof(DrawflowNodeMethodAttribute), false).Length > 0);

            foreach (var method in methodsWithAttr)
            {
                var nodeType = NodeType.Function;
                var section = "Default";
                var parameters = method.GetParameters();
                var output = method.ReturnParameter;
                List<DfPorts> dfOutputPorts = [];
                List<DfPorts> dfInputPorts = [];

                var functionAttribute = method.GetCustomAttribute(typeof(DrawflowNodeMethodAttribute)) as DrawflowNodeMethodAttribute;
                section = functionAttribute?.Section ?? section;
                nodeType = functionAttribute?.NodeType ?? nodeType;

                var paramsFromPorts = parameters.Where(x => !x.CustomAttributes.Any()
                    || x.CustomAttributes.All(attr => attr.AttributeType != typeof(DrawflowInputFieldAttribute)));

                var paramsFromInputFields = parameters.Where(x => x.CustomAttributes.Any(attr => attr.AttributeType == typeof(DrawflowInputFieldAttribute)));

                // NEW: read port metadata
                var flowPortsAttr = method.GetCustomAttribute<NodeFlowPortsAttribute>();
                var declaredPorts = flowPortsAttr?.Ports?.ToList() ?? new List<string>();

                var node = new Node
                {
                    Section = section,
                    BackingMethod = method,
                    DeclaredOutputPorts = declaredPorts
                };

                var serializedMethod = MethodInfoHelpers.ToSerializableString(method);

                nodes.Add(node);
            }

            return nodes;
        }

        public static async Task<int> CreateNodeV2(this DrawflowBase dfBase, Node node, string symbol)
        {
            var inputHtml = "";

            // 1 input for flow; you can later make this dynamic as well
            var inputs = 1;

            var outputs = (node.DeclaredOutputPorts != null && node.DeclaredOutputPorts.Count > 0)
                ? node.DeclaredOutputPorts.Count
                : 1;

            var nodeId = await dfBase.Editor!.AddNodeAsync(
                name: node.BackingMethod.Name,
                inputs: inputs,
                outputs: outputs,
                x: 480, y: 260,
                cssClass: "",
                data: new
                {
                    node,
                    outputPorts = node.DeclaredOutputPorts
                },
                html: $@"
                    <div class='node-type-id-container'>
                        <h5 class='node-type-id'>
                            {symbol}
                        </h5>
                    </div>
                    <div class='title-container'>
                        <div class='title' style='text-align: center;'>{node.BackingMethod.Name}</div>
                    </div>
                    <div class='main-content' style='min-width:300px'>
                        {inputHtml}
                    </div>
                    "
            );

            await dfBase.JS.InvokeVoidAsync("nextFrame");

            await dfBase.JS.InvokeVoidAsync("DrawflowBlazor.labelPorts", dfBase.Id, nodeId, new List<List<string>>(), node.DeclaredOutputPorts.Select(x => new List<string>() { x, "" } ));

            return nodeId ?? throw new InvalidOperationException("Failed to create node: AddNodeAsync returned null");
        }

        public static async Task SetNodeStatus(this DrawflowBase dfBase, string nodeId, NodeStatus nodeStatus)
        {
            await dfBase.JS.InvokeVoidAsync("DrawflowBlazor.setNodeStatus",
                dfBase.Id,
                nodeId,
                nodeStatus
            );
        }
    }
}
