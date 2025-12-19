namespace Umbraco.Ai.Agent.Core.Agents;

/// <summary>
/// Validates whether a agent execution is allowed based on its scope configuration.
/// </summary>
public interface IAiAgentScopeValidator
{
    /// <summary>
    /// Validates whether the agent execution is allowed for the given request context.
    /// When an EntityId is provided, validates against the actual content item.
    /// </summary>
    /// <param name="agent">The agent to validate.</param>
    /// <param name="request">The execution request with context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validation result indicating if execution is allowed.</returns>
    Task<AiAgentScopeValidationResult> ValidateAsync(
        AiAgent agent,
        AiAgentExecutionRequest request,
        CancellationToken cancellationToken = default);
}
