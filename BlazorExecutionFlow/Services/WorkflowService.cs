using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models;
using BlazorExecutionFlow.Models.NodeV2;
using BlazorExecutionFlow.Repositories;
using Microsoft.Extensions.Logging;

namespace BlazorExecutionFlow.Services
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

        public void SeedSampleWorkflowsIfEmpty()
        {
            if (!_repository.GetAll().Any())
            {
                SeedSampleWorkflows();
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

        private void SeedSampleWorkflows()
        {
            _logger?.LogInformation("Seeding sample workflows");

            var workflow1 = new WorkflowInfo
            {
                Id = "sample-1",
                Name = "Example: Data Processing Pipeline",
                Description = "Example workflow that fetches data from an API, processes it, and stores results",
                CreatedAt = DateTime.Now.AddDays(-10),
                ModifiedAt = DateTime.Now.AddDays(-2),
                Inputs = new Dictionary<string, string>
                {
                    ["apiUrl"] = "https://api.example.com/data",
                    ["maxRetries"] = "3",
                    ["timeout"] = "30000"
                },
                PreviousExecutions = new List<WorkflowExecution>
                {
                    new WorkflowExecution
                    {
                        Id = "exec-1",
                        ExecutedAt = DateTime.Now.AddHours(-2),
                        Success = true,
                        Duration = TimeSpan.FromSeconds(12.3),
                        Output = new Dictionary<string, object>
                        {
                            ["recordsProcessed"] = 1523,
                            ["status"] = "success"
                        }
                    },
                    new WorkflowExecution
                    {
                        Id = "exec-2",
                        ExecutedAt = DateTime.Now.AddHours(-5),
                        Success = false,
                        Duration = TimeSpan.FromSeconds(3.1),
                        ErrorMessage = "Connection timeout: Unable to reach API endpoint",
                        Output = new Dictionary<string, object>()
                    }
                },
                FlowGraph = new()
            };

            var workflow2 = new WorkflowInfo
            {
                Id = "sample-2",
                Name = "Example: Email Campaign Automation",
                Description = "Example workflow that sends personalized emails to subscribers based on their preferences",
                CreatedAt = DateTime.Now.AddDays(-5),
                ModifiedAt = DateTime.Now.AddDays(-1),
                Inputs = new Dictionary<string, string>
                {
                    ["campaignId"] = "camp-2024-01",
                    ["batchSize"] = "100"
                },
                PreviousExecutions = new List<WorkflowExecution>
                {
                    new WorkflowExecution
                    {
                        Id = "exec-3",
                        ExecutedAt = DateTime.Now.AddMinutes(-30),
                        Success = true,
                        Duration = TimeSpan.FromMinutes(2.5),
                        Output = new Dictionary<string, object>
                        {
                            ["emailsSent"] = 450,
                            ["bounces"] = 3,
                            ["opens"] = 127
                        }
                    }
                },
                FlowGraph = new()
            };

            var workflow3 = new WorkflowInfo
            {
                Id = "sample-3",
                Name = "Example: Report Generator",
                Description = "Example workflow that generates weekly analytics reports and uploads to cloud storage",
                CreatedAt = DateTime.Now.AddDays(-15),
                ModifiedAt = DateTime.Now.AddDays(-15),
                Inputs = new Dictionary<string, string>
                {
                    ["reportType"] = "weekly",
                    ["includeCharts"] = "true"
                },
                FlowGraph = new()
            };

            AddWorkflow(workflow1);
            AddWorkflow(workflow2);
            AddWorkflow(workflow3);
        }
    }
}
