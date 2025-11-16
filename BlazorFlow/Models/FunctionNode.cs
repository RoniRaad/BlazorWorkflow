namespace BlazorFlow.Models
{
    public class FunctionNode
    {
        public string Section { get; set; }
        public string Name { get; set; }
        public List<DfPorts> Inputs { get; set; } = [];
        public List<DfPorts> Outputs { get; set; } = [];
        public Dictionary<Type, string> FieldInputs { get; set; } = [];
        public NodeType Type { get; set; }
        public string? FullBackingFunctionAssemblyNameWithParams { get; set; }
    }

    public class DfPorts
    {
        public string Name { get; set; }= string.Empty;
        public DfPortType PortType { get; set; }
        public string TypeStringName { get; set; } = "unknown";
        public required Type BackingType { get; set; }
    }

    public enum DfPortType
    {
        String,
        Object,
        Integer,
        Boolean,
        Action,
        Null
    }

    public enum NodeType
    {
        Function,
        BooleanOperation,
        Variable,
        Loop,
        Event
    }
}
