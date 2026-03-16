namespace Umbraco.AI.Core.Guardrails.Resolvers;

/// <summary>
/// Result from a single guardrail resolver.
/// </summary>
public sealed class AIGuardrailResolverResult
{
    /// <summary>
    /// The resolved guardrail rules from this resolver.
    /// </summary>
    public IReadOnlyList<AIGuardrailRule> Rules { get; init; } = [];

    /// <summary>
    /// The guardrail IDs that were resolved (for deduplication tracking).
    /// </summary>
    public IReadOnlyList<Guid> GuardrailIds { get; init; } = [];

    /// <summary>
    /// The source name for debugging/tracking (e.g., profile name).
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Returns an empty result (no guardrails resolved).
    /// </summary>
    public static AIGuardrailResolverResult Empty => new();
}
