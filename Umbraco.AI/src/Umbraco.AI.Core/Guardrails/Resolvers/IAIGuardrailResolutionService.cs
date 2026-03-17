namespace Umbraco.AI.Core.Guardrails.Resolvers;

/// <summary>
/// Service that aggregates guardrail rules from all registered resolvers.
/// </summary>
public interface IAIGuardrailResolutionService
{
    /// <summary>
    /// Resolves all applicable guardrail rules by executing all registered resolvers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregated resolved guardrail rules.</returns>
    Task<AIResolvedGuardrails> ResolveGuardrailsAsync(CancellationToken cancellationToken = default);
}
