using System;
using System.Collections.Generic;
using BlazorExecutionFlow.Models.NodeV2;

namespace BlazorExecutionFlow.Models
{
    /// <summary>
    /// Represents metadata about a workflow
    /// </summary>
    public class WorkflowInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> Inputs { get; set; } = [];
        public List<WorkflowExecution> PreviousExecutions { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public required Graph FlowGraph { get; set; }
        public bool IncludeAsNode { get; set; } = false;
    }

    /// <summary>
    /// Represents a workflow execution record
    /// </summary>
    public class WorkflowExecution
    {
        public string Id { get; set; } = string.Empty;
        public DateTime ExecutedAt { get; set; }
        public Dictionary<string, object> Output { get; set; } = new();
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Event args for workflow-related events
    /// </summary>
    public class WorkflowEventArgs
    {
        public string WorkflowId { get; set; } = string.Empty;
        public WorkflowInfo? Workflow { get; set; }
    }

    /// <summary>
    /// Request to create a new workflow
    /// </summary>
    public class CreateWorkflowRequest
    {
        public string Name { get; set; } = "New Workflow";
        public string Description { get; set; } = string.Empty;
    }
}
