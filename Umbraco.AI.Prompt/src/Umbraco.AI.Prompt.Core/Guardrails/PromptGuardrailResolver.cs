using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Guardrails.Resolvers;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Prompt.Core.Prompts;
using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Prompt.Core.Guardrails;

/// <summary>
/// Resolves guardrails from prompt-level guardrail assignments.
/// </summary>
/// <remarks>
/// This resolver reads the prompt ID from <see cref="Constants.MetadataKeys.PromptId"/> in the runtime context,
/// then resolves any guardrail IDs configured on the prompt.
/// </remarks>
internal sealed class PromptGuardrailResolver : IAIGuardrailResolver
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIGuardrailService _guardrailService;
    private readonly IAIPromptService _promptService;

    public PromptGuardrailResolver(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIGuardrailService guardrailService,
        IAIPromptService promptService)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _guardrailService = guardrailService;
        _promptService = promptService;
    }

    /// <inheritdoc />
    public async Task<AIGuardrailResolverResult> ResolveAsync(CancellationToken cancellationToken = default)
    {
        // Override suppresses source-level guardrails entirely.
        if (_runtimeContextAccessor.Context?.GetValue<IReadOnlyList<Guid>>(CoreConstants.ContextKeys.GuardrailIdsOverride) is not null)
        {
            return AIGuardrailResolverResult.Empty;
        }

        var promptId = _runtimeContextAccessor.Context?.GetValue<Guid>(Constants.MetadataKeys.PromptId);
        if (!promptId.HasValue)
        {
            return AIGuardrailResolverResult.Empty;
        }

        var prompt = await _promptService.GetPromptAsync(promptId.Value, cancellationToken);
        if (prompt is null || prompt.GuardrailIds.Count == 0)
        {
            return AIGuardrailResolverResult.Empty;
        }

        var guardrails = await _guardrailService.GetGuardrailsByIdsAsync(prompt.GuardrailIds, cancellationToken);
        return AIGuardrailResolverResult.FromGuardrails(guardrails, source: prompt.Name);
    }
}
