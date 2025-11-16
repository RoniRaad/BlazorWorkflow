using BlazorFlow.Models;

namespace BlazorFlow.Drawflow.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class DrawflowNodeMethodAttribute : Attribute
    {
        public readonly NodeType NodeType;
        public string Section { get; set; }

        public DrawflowNodeMethodAttribute(NodeType nodeType, string section)
        {
            NodeType = nodeType;
            Section = section;
        }
    }
}
