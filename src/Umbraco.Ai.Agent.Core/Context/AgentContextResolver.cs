using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Core.Contexts;
using Umbraco.Ai.Core.Contexts.Resolvers;

namespace Umbraco.Ai.Agent.Core.Context;

/// <summary>
/// Resolves context from agent-level context assignments.
/// </summary>
/// <remarks>
/// This resolver reads the agent ID from <see cref="AgentIdKey"/> in the request properties,
/// then resolves any context IDs configured on the agent.
/// </remarks>
internal sealed class AgentContextResolver : IAiContextResolver
{
    /// <summary>
    /// Key used to pass the agent ID through ChatOptions.AdditionalProperties.
    /// </summary>
    internal const string AgentIdKey = "Umbraco.Ai.Agent.AgentId";

    private readonly IAiContextService _contextService;
    private readonly IAiAgentService _agentService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentContextResolver"/> class.
    /// </summary>
    /// <param name="contextService">The context service.</param>
    /// <param name="agentService">The agent service.</param>
    public AgentContextResolver(
        IAiContextService contextService,
        IAiAgentService agentService)
    {
        _contextService = contextService;
        _agentService = agentService;
    }

    /// <inheritdoc />
    public async Task<AiContextResolverResult> ResolveAsync(
        AiContextResolverRequest request,
        CancellationToken cancellationToken = default)
    {
        var agentId = request.GetGuidProperty(AgentIdKey);
        if (!agentId.HasValue)
        {
            return AiContextResolverResult.Empty;
        }

        var agent = await _agentService.GetAgentAsync(agentId.Value, cancellationToken);
        if (agent is null || agent.ContextIds.Count == 0)
        {
            return AiContextResolverResult.Empty;
        }

        return await ResolveContextIdsAsync(agent.ContextIds, agent.Name, cancellationToken);
    }

    private async Task<AiContextResolverResult> ResolveContextIdsAsync(
        IEnumerable<Guid> contextIds,
        string? entityName,
        CancellationToken cancellationToken)
    {
        var resources = new List<AiContextResolverResource>();
        var sources = new List<AiContextResolverSource>();

        foreach (var contextId in contextIds)
        {
            var context = await _contextService.GetContextAsync(contextId, cancellationToken);
            if (context is null)
            {
                continue;
            }

            sources.Add(new AiContextResolverSource(entityName, context.Name));

            foreach (var resource in context.Resources.OrderBy(r => r.SortOrder))
            {
                resources.Add(new AiContextResolverResource
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

        return new AiContextResolverResult
        {
            Resources = resources,
            Sources = sources
        };
    }
}
