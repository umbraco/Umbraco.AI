using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Guardrails.Resolvers;
using Umbraco.AI.Core.RuntimeContext;
using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Agent.Core.Guardrails;

/// <summary>
/// Resolves guardrails from agent-level guardrail assignments.
/// </summary>
/// <remarks>
/// This resolver reads the agent ID from <see cref="Constants.ContextKeys.AgentId"/> in the runtime context,
/// then resolves any guardrail IDs configured on the agent. Guardrails are available for all agent types
/// (standard and orchestrated).
/// </remarks>
internal sealed class AgentGuardrailResolver : IAIGuardrailResolver
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIGuardrailService _guardrailService;
    private readonly IAIAgentService _agentService;

    public AgentGuardrailResolver(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIGuardrailService guardrailService,
        IAIAgentService agentService)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _guardrailService = guardrailService;
        _agentService = agentService;
    }

    /// <inheritdoc />
    public async Task<AIGuardrailResolverResult> ResolveAsync(CancellationToken cancellationToken = default)
    {
        // Override suppresses source-level guardrails entirely.
        if (_runtimeContextAccessor.Context?.GetValue<IReadOnlyList<Guid>>(CoreConstants.ContextKeys.GuardrailIdsOverride) is not null)
        {
            return AIGuardrailResolverResult.Empty;
        }

        var agentId = _runtimeContextAccessor.Context?.GetValue<Guid>(Constants.ContextKeys.AgentId);
        if (!agentId.HasValue)
        {
            return AIGuardrailResolverResult.Empty;
        }

        var agent = await _agentService.GetAgentAsync(agentId.Value, cancellationToken);
        if (agent is null || agent.GuardrailIds.Count == 0)
        {
            return AIGuardrailResolverResult.Empty;
        }

        var guardrails = await _guardrailService.GetGuardrailsByIdsAsync(agent.GuardrailIds, cancellationToken);
        return AIGuardrailResolverResult.FromGuardrails(guardrails, source: agent.Name);
    }
}
