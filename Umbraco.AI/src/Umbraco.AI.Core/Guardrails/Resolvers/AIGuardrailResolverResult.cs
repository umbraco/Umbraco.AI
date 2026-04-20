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

    /// <summary>
    /// Projects a set of resolved guardrails into a result, flattening their rules (ordered by
    /// <see cref="AIGuardrailRule.SortOrder"/>) and stamping each rule with its parent guardrail name.
    /// </summary>
    /// <param name="guardrails">The guardrails to project.</param>
    /// <param name="source">The source name for debugging/tracking.</param>
    public static AIGuardrailResolverResult FromGuardrails(IEnumerable<AIGuardrail> guardrails, string? source)
    {
        var allRules = new List<AIGuardrailRule>();
        var resolvedIds = new List<Guid>();

        foreach (var guardrail in guardrails)
        {
            resolvedIds.Add(guardrail.Id);
            foreach (var rule in guardrail.Rules.OrderBy(r => r.SortOrder))
            {
                rule.GuardrailId = guardrail.Id;
                rule.GuardrailName = guardrail.Name;
                allRules.Add(rule);
            }
        }

        return new AIGuardrailResolverResult
        {
            Rules = allRules,
            GuardrailIds = resolvedIds,
            Source = source,
        };
    }
}
