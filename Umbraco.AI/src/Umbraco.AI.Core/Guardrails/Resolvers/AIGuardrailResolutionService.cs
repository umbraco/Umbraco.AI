namespace Umbraco.AI.Core.Guardrails.Resolvers;

/// <summary>
/// Default implementation of <see cref="IAIGuardrailResolutionService"/> that aggregates
/// guardrails from all registered resolvers.
/// </summary>
internal sealed class AIGuardrailResolutionService : IAIGuardrailResolutionService
{
    private readonly AIGuardrailResolverCollection _resolvers;

    public AIGuardrailResolutionService(AIGuardrailResolverCollection resolvers)
    {
        _resolvers = resolvers;
    }

    /// <inheritdoc />
    public async Task<AIResolvedGuardrails> ResolveGuardrailsAsync(CancellationToken cancellationToken = default)
    {
        var allRules = new List<AIGuardrailRule>();
        var seenGuardrailIds = new HashSet<Guid>();

        foreach (var resolver in _resolvers)
        {
            var result = await resolver.ResolveAsync(cancellationToken);

            // Deduplicate by guardrail ID (later resolvers don't duplicate earlier ones)
            foreach (var guardrailId in result.GuardrailIds)
            {
                if (!seenGuardrailIds.Add(guardrailId))
                {
                    continue;
                }
            }

            // Add rules from guardrails we haven't seen before
            foreach (var rule in result.Rules)
            {
                allRules.Add(rule);
            }
        }

        return new AIResolvedGuardrails
        {
            AllRules = allRules,
            PreGenerateRules = allRules.Where(r => r.Phase == AIGuardrailPhase.PreGenerate).ToList(),
            PostGenerateRules = allRules.Where(r => r.Phase == AIGuardrailPhase.PostGenerate).ToList()
        };
    }
}
