using System.Text;
using BlazorWorkflow.Helpers;
using BlazorWorkflow.Models;
using BlazorWorkflow.Models.NodeV2;
using BlazorWorkflow.Repositories;
using Microsoft.Extensions.Logging;

namespace BlazorWorkflow.Services
{
    /// <summary>
    /// Default implementation of IWorkflowService.
    /// Provides business logic for workflow management using a repository for data access.
    /// </summary>
    public class WorkflowService : IWorkflowService
    {
        private readonly IWorkflowRepository _repository;
        private readonly ILogger<WorkflowService>? _logger;

        public WorkflowService(IWorkflowRepository repository, ILogger<WorkflowService>? logger = null)
        {
            _repository = repository;
            _logger = logger;
        }

        public List<WorkflowInfo> GetAllWorkflows()
        {
            var workflows = _repository.GetAll();
            foreach (var workflow in workflows)
            {
                PopulateInputData(workflow);
            }
            return workflows;
        }

        public WorkflowInfo? GetWorkflow(string id)
        {
            var workflow = _repository.GetById(id);
            if (workflow != null)
            {
                PopulateInputData(workflow);
            }
            return workflow;
        }

        public void AddWorkflow(WorkflowInfo workflow)
        {
            _repository.Add(workflow);
        }

        public void UpdateWorkflow(WorkflowInfo workflow)
        {
            _repository.Update(workflow);
        }

        public void DeleteWorkflow(string id)
        {
            _repository.Delete(id);
        }

        public void SeedFromTemplatesIfEmpty(List<string> base64Templates)
        {
            if (_repository.GetAll().Any())
                return;

            if (base64Templates.Count == 0)
            {
                _logger?.LogInformation("No seed workflow templates provided, skipping seed");
                return;
            }

            foreach (var template in base64Templates)
            {
                if (string.IsNullOrWhiteSpace(template))
                    continue;

                try
                {
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(template.Trim()));
                    var nodes = FlowSerializer.DeserializeFlow(json, out var metadata);

                    if (nodes.Count == 0)
                    {
                        _logger?.LogWarning("Seed workflow template contained no valid nodes, skipping");
                        continue;
                    }

                    var workflow = new WorkflowInfo
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = metadata.FlowName ?? "Sample Workflow",
                        Description = string.Empty,
                        CreatedAt = DateTime.Now,
                        ModifiedAt = DateTime.Now,
                        FlowGraph = new Graph()
                    };

                    workflow.FlowGraph.Nodes.Clear();
                    foreach (var node in nodes)
                    {
                        workflow.FlowGraph.Nodes[node.DrawflowNodeId] = node;
                    }

                    AddWorkflow(workflow);
                    _logger?.LogInformation("Seeded workflow '{Name}' from template", workflow.Name);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to seed workflow from template");
                }
            }
        }

        /// <summary>
        /// Populates input data for workflow nodes, particularly for workflow-as-node scenarios.
        /// </summary>
        private void PopulateInputData(WorkflowInfo workflow)
        {
            foreach (var nodeKvp in workflow.FlowGraph.Nodes)
            {
                var node = nodeKvp.Value;
                if (node.IsWorkflowNode)
                {
                    var externalWorkflow = GetWorkflow(node.ParentWorkflowId!);
                    var discoveredInputs = WorkflowInputDiscovery.DiscoverInputs(externalWorkflow?.FlowGraph ?? new());
                    var newInputMap = new List<PathMapEntry>();
                    foreach (var input in discoveredInputs)
                    {
                        var currentMap = node.NodeInputToMethodInputMap.FirstOrDefault(x => x.To == input);
                        if (currentMap == null)
                        {
                            newInputMap.Add(new PathMapEntry() { To = input });
                        }
                        else
                        {
                            newInputMap.Add(currentMap);
                        }
                    }

                    // We replace it so that if the inputs on the workflow are changed stale input maps are removed.
                    node.NodeInputToMethodInputMap = newInputMap;
                }
            }
        }

    }
}
