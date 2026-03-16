using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Guardrails.Resolvers;

/// <summary>
/// Resolves guardrails from an explicit override in the runtime context.
/// </summary>
/// <remarks>
/// <para>
/// This resolver reads <see cref="Constants.ContextKeys.GuardrailIdsOverride"/> from the runtime context.
/// When present, it resolves the specified guardrails. Other resolvers still run and their results
/// are deduplicated by the resolution service.
/// </para>
/// <para>
/// This resolver is registered first in the pipeline so its guardrail IDs take priority during deduplication.
/// </para>
/// </remarks>
internal sealed class GuardrailIdsOverrideResolver : IAIGuardrailResolver
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIGuardrailService _guardrailService;

    public GuardrailIdsOverrideResolver(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIGuardrailService guardrailService)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _guardrailService = guardrailService;
    }

    /// <inheritdoc />
    public async Task<AIGuardrailResolverResult> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var guardrailIds = _runtimeContextAccessor.Context?.GetValue<IReadOnlyList<Guid>>(Constants.ContextKeys.GuardrailIdsOverride);
        if (guardrailIds is null || guardrailIds.Count == 0)
        {
            return AIGuardrailResolverResult.Empty;
        }

        var guardrails = await _guardrailService.GetGuardrailsByIdsAsync(guardrailIds, cancellationToken);

        var allRules = new List<AIGuardrailRule>();
        var resolvedIds = new List<Guid>();

        foreach (var guardrail in guardrails)
        {
            resolvedIds.Add(guardrail.Id);
            foreach (var rule in guardrail.Rules.OrderBy(r => r.SortOrder))
            {
                rule.GuardrailName = guardrail.Name;
                allRules.Add(rule);
            }
        }

        return new AIGuardrailResolverResult
        {
            Rules = allRules,
            GuardrailIds = resolvedIds,
            Source = "Override"
        };
    }
}
