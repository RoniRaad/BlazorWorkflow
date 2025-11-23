namespace BlazorExecutionFlow.Services
{
    /// <summary>
    /// Service for prompting users for input during workflow execution.
    /// </summary>
    public interface IUserPromptService
    {
        /// <summary>
        /// Prompts the user for input and waits for their response.
        /// </summary>
        /// <param name="message">The message to display to the user</param>
        /// <param name="defaultValue">The default value to use if the user cancels</param>
        /// <returns>The user's input, or the default value if cancelled</returns>
        Task<string?> PromptUserAsync(string message, string? defaultValue = null);
    }
}
