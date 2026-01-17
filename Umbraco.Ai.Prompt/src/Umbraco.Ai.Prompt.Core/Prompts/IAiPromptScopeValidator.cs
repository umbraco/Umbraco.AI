namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Validates whether a prompt execution is allowed based on its scope configuration.
/// </summary>
public interface IAiPromptScopeValidator
{
    /// <summary>
    /// Validates whether the prompt execution is allowed for the given request context.
    /// When an EntityId is provided, validates against the actual content item.
    /// </summary>
    /// <param name="prompt">The prompt to validate.</param>
    /// <param name="request">The execution request with context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validation result indicating if execution is allowed.</returns>
    Task<AiPromptScopeValidationResult> ValidateAsync(
        AiPrompt prompt,
        AiPromptExecutionRequest request,
        CancellationToken cancellationToken = default);
}
