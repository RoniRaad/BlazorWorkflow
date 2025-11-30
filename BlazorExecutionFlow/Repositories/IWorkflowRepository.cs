using BlazorExecutionFlow.Models;

namespace BlazorExecutionFlow.Repositories
{
    /// <summary>
    /// Repository interface for workflow data access.
    /// Handles the persistence and retrieval of workflow data.
    /// </summary>
    public interface IWorkflowRepository
    {
        /// <summary>
        /// Gets all workflows from storage.
        /// </summary>
        List<WorkflowInfo> GetAll();

        /// <summary>
        /// Gets a specific workflow by ID from storage.
        /// </summary>
        WorkflowInfo? GetById(string id);

        /// <summary>
        /// Adds a new workflow to storage.
        /// </summary>
        void Add(WorkflowInfo workflow);

        /// <summary>
        /// Updates an existing workflow in storage.
        /// </summary>
        void Update(WorkflowInfo workflow);

        /// <summary>
        /// Deletes a workflow by ID from storage.
        /// </summary>
        void Delete(string id);
    }
}
