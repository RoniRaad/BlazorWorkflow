using BlazorFlow.Models;

namespace BlazorFlow.Drawflow.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class DrawflowNodeBranchingMethodAttribute : Attribute
    {
        public readonly NodeType NodeType;
        public string Section { get; set; }

        public DrawflowNodeBranchingMethodAttribute(NodeType nodeType, string section)
        {
            NodeType = nodeType;
            Section = section;
        }
    }
}
