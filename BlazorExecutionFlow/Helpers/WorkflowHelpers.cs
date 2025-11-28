using System.Collections.Frozen;
using System.Text.Json.Nodes;
using BlazorExecutionFlow.Models;
using BlazorExecutionFlow.Models.NodeV2;

namespace BlazorExecutionFlow.Helpers
{
    public static class WorkflowHelpers
    {
        public static async Task<JsonObject> ExecuteWorkflow(WorkflowInfo workflow, 
            Dictionary<string, string> inputParams, Dictionary<string, string> envVariables)
        {
            var context = new GraphExecutionContext()
            {
                Parameters = inputParams.ToFrozenDictionary(),
                EnvironmentVariables = envVariables.ToFrozenDictionary()
            };
            await workflow.FlowGraph.Run(context);

            return context.SharedContext;
        }
    }
}
