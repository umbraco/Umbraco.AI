using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Web.Api.Common.Models;

namespace Umbraco.Ai.Agent.Extensions;

/// <summary>
/// Extension methods for <see cref="IAiAgentService"/> to support IdOrAlias lookups.
/// </summary>
internal static class AgentServiceExtensions
{
    /// <summary>
    /// Gets a agent by ID or alias.
    /// </summary>
    /// <param name="service">The agent service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent if found, otherwise null.</returns>
    public static async Task<Core.Agents.AiAgent?> GetAgentAsync(
        this IAiAgentService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return await service.GetAgentAsync(idOrAlias.Id.Value, cancellationToken);
        }

        if (idOrAlias.Alias != null)
        {
            return await service.GetAgentByAliasAsync(idOrAlias.Alias, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Tries to get a agent ID by ID or alias.
    /// If already an ID, returns it directly without a database lookup.
    /// </summary>
    /// <param name="service">The agent service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent ID if found, otherwise null.</returns>
    public static async Task<Guid?> TryGetAgentIdAsync(
        this IAiAgentService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        // If it's already an ID, return it directly (no DB lookup needed)
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return idOrAlias.Id.Value;
        }

        // For alias, we need to look up the agent
        if (idOrAlias.Alias != null)
        {
            var agent = await service.GetAgentByAliasAsync(idOrAlias.Alias, cancellationToken);
            return agent?.Id;
        }

        return null;
    }

    /// <summary>
    /// Gets a agent ID by ID or alias, throwing if not found.
    /// </summary>
    /// <param name="service">The agent service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the agent is not found.</exception>
    public static async Task<Guid> GetAgentIdAsync(
        this IAiAgentService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        var agentId = await TryGetAgentIdAsync(service, idOrAlias, cancellationToken);
        if (agentId is null)
        {
            throw new InvalidOperationException(
                $"Unable to find a agent with the id or alias of '{idOrAlias}'");
        }

        return agentId.Value;
    }
}
