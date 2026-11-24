namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Thrown when a prompt exists but the caller may not run it, because the prompt is limited to user
/// groups the caller is not in. Callers map it to "Prompt not available" (403 in the Management API).
/// </summary>
/// <remarks>
/// Derives from <see cref="InvalidOperationException"/>, like the other execution refusals (prompt not found,
/// scope denied, cancelled), so existing handlers for those keep treating it as a failed execution.
/// </remarks>
public sealed class AIPromptNotAvailableException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIPromptNotAvailableException"/> class.
    /// </summary>
    /// <param name="promptId">The ID of the prompt the caller may not run.</param>
    public AIPromptNotAvailableException(Guid promptId)
        : base($"Prompt '{promptId}' is not available.")
    {
        PromptId = promptId;
    }

    /// <summary>
    /// Gets the ID of the prompt the caller may not run.
    /// </summary>
    public Guid PromptId { get; }
}
