namespace BlazorExecutionFlow.Drawflow.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class NodeFlowPortsAttribute : Attribute
    {
        public IReadOnlyList<string> Ports { get; }

        public NodeFlowPortsAttribute(params string[] ports)
        {
            Ports = ports?.Length > 0 ? ports : Array.Empty<string>();
        }
    }
}
