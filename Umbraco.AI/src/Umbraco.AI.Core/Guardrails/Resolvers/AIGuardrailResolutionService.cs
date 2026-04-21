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

            // Group rules by parent guardrail so we can skip duplicates at rule granularity — a resolver
            // may return rules whose guardrail was already contributed by an earlier resolver.
            foreach (var group in result.Rules.GroupBy(r => r.GuardrailId))
            {
                if (group.Key is Guid guardrailId && !seenGuardrailIds.Add(guardrailId))
                {
                    continue;
                }

                allRules.AddRange(group);
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
