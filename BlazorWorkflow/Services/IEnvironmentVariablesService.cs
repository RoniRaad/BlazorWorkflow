namespace BlazorWorkflow.Services
{
    /// <summary>
    /// Service for managing environment variables across different environments.
    /// Consumers can implement this interface to provide their own storage mechanism.
    /// </summary>
    public interface IEnvironmentVariablesService
    {
        /// <summary>
        /// Gets all environment names.
        /// </summary>
        List<string> GetAllEnvironments();

        /// <summary>
        /// Adds a new environment.
        /// </summary>
        void AddEnvironment(string environmentName);

        /// <summary>
        /// Removes an environment and all its variables.
        /// </summary>
        void RemoveEnvironment(string environmentName);

        /// <summary>
        /// Renames an environment.
        /// </summary>
        void RenameEnvironment(string oldName, string newName);

        /// <summary>
        /// Gets all variables for a specific environment.
        /// </summary>
        Dictionary<string, string> GetAllVariables(string environmentName);

        /// <summary>
        /// Gets a specific variable value.
        /// </summary>
        string? GetVariable(string environmentName, string key);

        /// <summary>
        /// Sets a variable value.
        /// </summary>
        void SetVariable(string environmentName, string key, string value);

        /// <summary>
        /// Removes a variable.
        /// </summary>
        void RemoveVariable(string environmentName, string key);

        /// <summary>
        /// Renames a variable key while preserving its position in the collection.
        /// </summary>
        void RenameVariable(string environmentName, string oldKey, string newKey);

        /// <summary>
        /// Checks if a variable exists.
        /// </summary>
        bool VariableExists(string environmentName, string key);
    }
}
