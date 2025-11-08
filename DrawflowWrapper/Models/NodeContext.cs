using DrawflowWrapper.Models.NodeV2;

namespace DrawflowWrapper.Models
{
    public class NodeContext
    {
        public required Node InputNode { get; set; }
        public required Node OuputNode { get; set; }
        public Dictionary<string, object> Context { get; set; } = [];
    }
}
