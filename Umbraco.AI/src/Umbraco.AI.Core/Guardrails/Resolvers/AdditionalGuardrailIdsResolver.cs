using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Guardrails.Resolvers;

/// <summary>
/// Resolves additional guardrails appended by the caller via the builder's <c>WithGuardrails</c> API.
/// </summary>
/// <remarks>
/// <para>
/// This resolver reads <see cref="Constants.ContextKeys.AdditionalGuardrailIds"/> from the runtime context
/// and adds the specified guardrails to whatever the source resolvers (profile/agent/prompt) produced. If a
/// full <see cref="Constants.ContextKeys.GuardrailIdsOverride"/> is also set, the additional IDs are still
/// applied on top of the override.
/// </para>
/// </remarks>
internal sealed class AdditionalGuardrailIdsResolver : IAIGuardrailResolver
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIGuardrailService _guardrailService;

    public AdditionalGuardrailIdsResolver(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIGuardrailService guardrailService)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _guardrailService = guardrailService;
    }

    /// <inheritdoc />
    public async Task<AIGuardrailResolverResult> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var guardrailIds = _runtimeContextAccessor.Context?.GetValue<IReadOnlyList<Guid>>(Constants.ContextKeys.AdditionalGuardrailIds);
        if (guardrailIds is null || guardrailIds.Count == 0)
        {
            return AIGuardrailResolverResult.Empty;
        }

        var guardrails = await _guardrailService.GetGuardrailsByIdsAsync(guardrailIds, cancellationToken);
        return AIGuardrailResolverResult.FromGuardrails(guardrails, source: "Additional");
    }
}
