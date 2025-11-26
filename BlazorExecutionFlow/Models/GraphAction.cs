using BlazorExecutionFlow.Models.NodeV2;

namespace BlazorExecutionFlow.Models
{
    /// <summary>
    /// Base class for graph actions that can be undone/redone.
    /// </summary>
    public abstract class GraphAction
    {
        /// <summary>
        /// Execute this action (redo).
        /// </summary>
        public abstract Task ExecuteAsync();

        /// <summary>
        /// Undo this action.
        /// </summary>
        public abstract Task UndoAsync();
    }

    /// <summary>
    /// Action for when a node is moved.
    /// </summary>
    public class NodeMovedAction : GraphAction
    {
        private readonly Node _node;
        private readonly double _oldPosX;
        private readonly double _oldPosY;
        private readonly double _newPosX;
        private readonly double _newPosY;
        private readonly Func<string, double, double, Task> _moveNodeFunc;

        public NodeMovedAction(Node node, double oldPosX, double oldPosY, double newPosX, double newPosY, Func<string, double, double, Task> moveNodeFunc)
        {
            _node = node;
            _oldPosX = oldPosX;
            _oldPosY = oldPosY;
            _newPosX = newPosX;
            _newPosY = newPosY;
            _moveNodeFunc = moveNodeFunc;
        }

        public override async Task ExecuteAsync()
        {
            _node.PosX = _newPosX;
            _node.PosY = _newPosY;
            await _moveNodeFunc(_node.DrawflowNodeId, _newPosX, _newPosY);
        }

        public override async Task UndoAsync()
        {
            _node.PosX = _oldPosX;
            _node.PosY = _oldPosY;
            await _moveNodeFunc(_node.DrawflowNodeId, _oldPosX, _oldPosY);
        }
    }

    /// <summary>
    /// Action for when a connection is created.
    /// </summary>
    public class ConnectionCreatedAction : GraphAction
    {
        private readonly Node _sourceNode;
        private readonly Node _targetNode;
        private readonly string _outputPortName;
        private readonly Func<string, string, string, Task> _addConnectionFunc;
        private readonly Func<string, string, string, Task> _removeConnectionFunc;

        public ConnectionCreatedAction(
            Node sourceNode,
            Node targetNode,
            string outputPortName,
            Func<string, string, string, Task> addConnectionFunc,
            Func<string, string, string, Task> removeConnectionFunc)
        {
            _sourceNode = sourceNode;
            _targetNode = targetNode;
            _outputPortName = outputPortName;
            _addConnectionFunc = addConnectionFunc;
            _removeConnectionFunc = removeConnectionFunc;
        }

        public override async Task ExecuteAsync()
        {
            // Add connection to graph model
            _sourceNode.AddOutputConnection(_outputPortName, _targetNode);
            if (!_targetNode.InputNodes.Contains(_sourceNode))
            {
                _targetNode.InputNodes.Add(_sourceNode);
            }

            // Add connection in UI
            await _addConnectionFunc(_sourceNode.DrawflowNodeId, _targetNode.DrawflowNodeId, _outputPortName);
        }

        public override async Task UndoAsync()
        {
            // Remove connection from graph model
            if (_sourceNode.OutputPorts.TryGetValue(_outputPortName, out var targets))
            {
                targets.Remove(_targetNode);
                if (targets.Count == 0)
                {
                    _sourceNode.OutputPorts.Remove(_outputPortName);
                }
            }
            _sourceNode.OutputNodes.Remove(_targetNode);

            // Check if targetNode still has other connections from sourceNode
            bool hasOtherConnections = false;
            foreach (var portTargets in _sourceNode.OutputPorts.Values)
            {
                if (portTargets.Contains(_targetNode))
                {
                    hasOtherConnections = true;
                    break;
                }
            }

            if (!hasOtherConnections)
            {
                _targetNode.InputNodes.Remove(_sourceNode);
            }

            // Remove connection from UI
            await _removeConnectionFunc(_sourceNode.DrawflowNodeId, _targetNode.DrawflowNodeId, _outputPortName);
        }
    }

    /// <summary>
    /// Action for when a connection is removed.
    /// </summary>
    public class ConnectionRemovedAction : GraphAction
    {
        private readonly Node _sourceNode;
        private readonly Node _targetNode;
        private readonly string _outputPortName;
        private readonly Func<string, string, string, Task> _addConnectionFunc;
        private readonly Func<string, string, string, Task> _removeConnectionFunc;

        public ConnectionRemovedAction(
            Node sourceNode,
            Node targetNode,
            string outputPortName,
            Func<string, string, string, Task> addConnectionFunc,
            Func<string, string, string, Task> removeConnectionFunc)
        {
            _sourceNode = sourceNode;
            _targetNode = targetNode;
            _outputPortName = outputPortName;
            _addConnectionFunc = addConnectionFunc;
            _removeConnectionFunc = removeConnectionFunc;
        }

        public override async Task ExecuteAsync()
        {
            // Remove connection from graph model
            if (_sourceNode.OutputPorts.TryGetValue(_outputPortName, out var targets))
            {
                targets.Remove(_targetNode);
                if (targets.Count == 0)
                {
                    _sourceNode.OutputPorts.Remove(_outputPortName);
                }
            }
            _sourceNode.OutputNodes.Remove(_targetNode);

            // Check if targetNode still has other connections from sourceNode
            bool hasOtherConnections = false;
            foreach (var portTargets in _sourceNode.OutputPorts.Values)
            {
                if (portTargets.Contains(_targetNode))
                {
                    hasOtherConnections = true;
                    break;
                }
            }

            if (!hasOtherConnections)
            {
                _targetNode.InputNodes.Remove(_sourceNode);
            }

            // Remove connection from UI
            await _removeConnectionFunc(_sourceNode.DrawflowNodeId, _targetNode.DrawflowNodeId, _outputPortName);
        }

        public override async Task UndoAsync()
        {
            // Add connection back to graph model
            _sourceNode.AddOutputConnection(_outputPortName, _targetNode);
            if (!_targetNode.InputNodes.Contains(_sourceNode))
            {
                _targetNode.InputNodes.Add(_sourceNode);
            }

            // Add connection back to UI
            await _addConnectionFunc(_sourceNode.DrawflowNodeId, _targetNode.DrawflowNodeId, _outputPortName);
        }
    }

    /// <summary>
    /// Action for when a node is removed.
    /// </summary>
    public class NodeRemovedAction : GraphAction
    {
        private readonly Graph _graph;
        private readonly Node _node;
        private readonly List<(Node source, Node target, string port)> _connections;
        private readonly Func<Node, Task<string>> _addNodeFunc;
        private readonly Func<string, Task> _removeNodeFunc;
        private readonly Func<string, string, string, Task> _addConnectionFunc;

        public NodeRemovedAction(
            Graph graph,
            Node node,
            Func<Node, Task<string>> addNodeFunc,
            Func<string, Task> removeNodeFunc,
            Func<string, string, string, Task> addConnectionFunc)
        {
            _graph = graph;
            _node = node;
            _addNodeFunc = addNodeFunc;
            _removeNodeFunc = removeNodeFunc;
            _addConnectionFunc = addConnectionFunc;

            // Store all connections for this node
            _connections = new List<(Node source, Node target, string port)>();

            // Store outgoing connections
            foreach (var port in node.OutputPorts)
            {
                foreach (var target in port.Value)
                {
                    _connections.Add((node, target, port.Key));
                }
            }

            // Store incoming connections
            foreach (var otherNode in graph.Nodes.Values)
            {
                if (otherNode != node)
                {
                    foreach (var port in otherNode.OutputPorts)
                    {
                        if (port.Value.Contains(node))
                        {
                            _connections.Add((otherNode, node, port.Key));
                        }
                    }
                }
            }
        }

        public override async Task ExecuteAsync()
        {
            // Remove all connections
            foreach (var otherNode in _graph.Nodes.Values)
            {
                if (otherNode != _node)
                {
                    // Remove outgoing connections from other nodes to this node
                    foreach (var port in otherNode.OutputPorts.Values)
                    {
                        port.Remove(_node);
                    }
                    otherNode.OutputNodes.Remove(_node);

                    // Remove this node from other nodes' InputNodes
                    otherNode.InputNodes.Remove(_node);
                }
            }

            // Remove node from graph
            _graph.Nodes.Remove(_node.Id, out _);

            // Remove node from UI
            await _removeNodeFunc(_node.DrawflowNodeId);
        }

        public override async Task UndoAsync()
        {
            // Add node back to graph
            _graph.Nodes[_node.Id] = _node;

            // Add node back to UI
            var newDrawflowId = await _addNodeFunc(_node);
            _node.DrawflowNodeId = newDrawflowId;

            // Restore all connections
            foreach (var (source, target, port) in _connections)
            {
                source.AddOutputConnection(port, target);
                if (!target.InputNodes.Contains(source))
                {
                    target.InputNodes.Add(source);
                }

                // Restore connection in UI
                await _addConnectionFunc(source.DrawflowNodeId, target.DrawflowNodeId, port);
            }
        }
    }
}
