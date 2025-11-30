using BlazorExecutionFlow.Models;

namespace BlazorExecutionFlow.Services
{
    /// <summary>
    /// Service for managing workflows with business logic.
    /// Handles workflow operations including input discovery and validation.
    /// </summary>
    public interface IWorkflowService
    {
        /// <summary>
        /// Gets all workflows with populated input data.
        /// </summary>
        List<WorkflowInfo> GetAllWorkflows();

        /// <summary>
        /// Gets a specific workflow by ID with populated input data.
        /// </summary>
        WorkflowInfo? GetWorkflow(string id);

        /// <summary>
        /// Adds a new workflow.
        /// </summary>
        void AddWorkflow(WorkflowInfo workflow);

        /// <summary>
        /// Updates an existing workflow.
        /// </summary>
        void UpdateWorkflow(WorkflowInfo workflow);

        /// <summary>
        /// Deletes a workflow by ID.
        /// </summary>
        void DeleteWorkflow(string id);

        /// <summary>
        /// Seeds sample workflows if none exist.
        /// </summary>
        void SeedSampleWorkflowsIfEmpty();
    }
}
