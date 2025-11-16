using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorFlow.Drawflow.Attributes
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
