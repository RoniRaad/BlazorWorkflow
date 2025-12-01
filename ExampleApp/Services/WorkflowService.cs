using BlazorExecutionFlow.Models;

namespace ExampleApp.Services;

/// <summary>
/// In-memory workflow storage service for demonstration purposes.
/// In a real application, this would be backed by a database.
/// </summary>
public class WorkflowService
{
    private readonly List<WorkflowInfo> _workflows = new();

    public WorkflowService()
    {
    }

    public List<WorkflowInfo> GetAllWorkflows()
    {
        return _workflows.ToList();
    }

    public WorkflowInfo? GetWorkflow(string id)
    {
        return _workflows.FirstOrDefault(w => w.Id == id);
    }

    public void AddWorkflow(WorkflowInfo workflow)
    {
        _workflows.Add(workflow);
    }

    public void UpdateWorkflow(WorkflowInfo workflow)
    {
        var existing = _workflows.FirstOrDefault(w => w.Id == workflow.Id);
        if (existing != null)
        {
            var index = _workflows.IndexOf(existing);
            _workflows[index] = workflow;
        }
    }

    public void DeleteWorkflow(string id)
    {
        var workflow = _workflows.FirstOrDefault(w => w.Id == id);
        if (workflow != null)
        {
            _workflows.Remove(workflow);
        }
    }

    private void SeedSampleWorkflows()
    {
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

        _workflows.Add(workflow1);
        _workflows.Add(workflow2);
        _workflows.Add(workflow3);
    }
}
