using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.Resolvers;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Agent.Core.Context;

/// <summary>
/// Resolves context from agent-level context assignments.
/// </summary>
/// <remarks>
/// This resolver reads the agent ID from <see cref="Constants.MetadataKeys.AgentId"/> in the request properties,
/// then resolves any context IDs configured on the agent.
/// </remarks>
internal sealed class AgentContextResolver : IAiContextResolver
{
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAiContextService _contextService;
    private readonly IAiAgentService _agentService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentContextResolver"/> class.
    /// </summary>
    /// <param name="runtimeContextAccessor">The runtime context accessor.</param>
    /// <param name="contextService">The context service.</param>
    /// <param name="agentService">The agent service.</param>
    public AgentContextResolver(
        IAiRuntimeContextAccessor runtimeContextAccessor,
        IAiContextService contextService,
        IAiAgentService agentService)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _contextService = contextService;
        _agentService = agentService;
    }

    /// <inheritdoc />
    public async Task<AIContextResolverResult> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var agentId = _runtimeContextAccessor.Context?.GetValue<Guid>(Constants.ContextKeys.AgentId);
        if (!agentId.HasValue)
        {
            return AIContextResolverResult.Empty;
        }

        var agent = await _agentService.GetAgentAsync(agentId.Value, cancellationToken);
        if (agent is null || agent.ContextIds.Count == 0)
        {
            return AIContextResolverResult.Empty;
        }

        return await ResolveContextIdsAsync(agent.ContextIds, agent.Name, cancellationToken);
    }

    private async Task<AIContextResolverResult> ResolveContextIdsAsync(
        IEnumerable<Guid> contextIds,
        string? entityName,
        CancellationToken cancellationToken)
    {
        var resources = new List<AIContextResolverResource>();
        var sources = new List<AIContextResolverSource>();

        foreach (var contextId in contextIds)
        {
            var context = await _contextService.GetContextAsync(contextId, cancellationToken);
            if (context is null)
            {
                continue;
            }

            sources.Add(new AIContextResolverSource(entityName, context.Name));

            foreach (var resource in context.Resources.OrderBy(r => r.SortOrder))
            {
                resources.Add(new AIContextResolverResource
                {
                    Id = resource.Id,
                    ResourceTypeId = resource.ResourceTypeId,
                    Name = resource.Name,
                    Description = resource.Description,
                    Data = resource.Data,
                    InjectionMode = resource.InjectionMode,
                    ContextName = context.Name
                });
            }
        }

        return new AIContextResolverResult
        {
            Resources = resources,
            Sources = sources
        };
    }
}
