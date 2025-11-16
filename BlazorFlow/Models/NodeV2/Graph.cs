using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorFlow.Models.NodeV2
{
    public class Graph
    {
        public ConcurrentDictionary<string, Node> Nodes = [];
    }
}
