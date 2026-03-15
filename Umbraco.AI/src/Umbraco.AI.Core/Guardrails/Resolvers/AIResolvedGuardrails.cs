namespace Umbraco.AI.Core.Guardrails.Resolvers;

/// <summary>
/// Aggregated result from all guardrail resolvers.
/// </summary>
public sealed class AIResolvedGuardrails
{
    /// <summary>
    /// All resolved rules across all resolvers, deduplicated by guardrail ID.
    /// </summary>
    public IReadOnlyList<AIGuardrailRule> AllRules { get; init; } = [];

    /// <summary>
    /// Rules that should run in the pre-generate phase.
    /// </summary>
    public IReadOnlyList<AIGuardrailRule> PreGenerateRules { get; init; } = [];

    /// <summary>
    /// Rules that should run in the post-generate phase.
    /// </summary>
    public IReadOnlyList<AIGuardrailRule> PostGenerateRules { get; init; } = [];

    /// <summary>
    /// Whether there are any rules to evaluate.
    /// </summary>
    public bool HasRules => AllRules.Count > 0;

    /// <summary>
    /// Returns an empty result with no rules.
    /// </summary>
    public static AIResolvedGuardrails Empty => new();
}
