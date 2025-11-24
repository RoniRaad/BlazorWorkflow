using System.Collections.Concurrent;

namespace BlazorExecutionFlow.Services
{
    /// <summary>
    /// Implementation of IUserPromptService that uses events to communicate with the UI.
    /// </summary>
    public class UserPromptService : IUserPromptService
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string?>> _pendingPrompts = new();

        /// <summary>
        /// Event raised when a prompt is requested. The UI should subscribe to this event.
        /// </summary>
        public event EventHandler<UserPromptRequestedEventArgs>? PromptRequested;

        /// <summary>
        /// Prompts the user for input and waits for their response.
        /// </summary>
        public async Task<string?> PromptUserAsync(string message, string? defaultValue = null)
        {
            var promptId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<string?>();

            _pendingPrompts[promptId] = tcs;

            try
            {
                // Raise event to show the prompt in the UI
                var args = new UserPromptRequestedEventArgs
                {
                    PromptId = promptId,
                    Message = message,
                    DefaultValue = defaultValue ?? string.Empty
                };

                PromptRequested?.Invoke(this, args);

                // Wait for the user's response
                var result = await tcs.Task;
                return result;
            }
            finally
            {
                _pendingPrompts.TryRemove(promptId, out _);
            }
        }

        /// <summary>
        /// Called by the UI when the user provides a response.
        /// </summary>
        public void SubmitResponse(string promptId, string? value)
        {
            if (_pendingPrompts.TryGetValue(promptId, out var tcs))
            {
                tcs.TrySetResult(value);
            }
        }

        /// <summary>
        /// Called by the UI when the user cancels the prompt.
        /// </summary>
        public void CancelPrompt(string promptId)
        {
            if (_pendingPrompts.TryGetValue(promptId, out var tcs))
            {
                tcs.TrySetResult(null);
            }
        }
    }

    /// <summary>
    /// Event args for when a user prompt is requested.
    /// </summary>
    public class UserPromptRequestedEventArgs : EventArgs
    {
        public required string PromptId { get; init; }
        public required string Message { get; init; }
        public required string DefaultValue { get; init; }
    }
}
