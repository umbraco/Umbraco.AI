using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Web.Api.Common.Models;

namespace Umbraco.AI.Agent.Extensions;

/// <summary>
/// Extension methods for <see cref="IAIAgentService"/> to support IdOrAlias lookups.
/// </summary>
internal static class AgentServiceExtensions
{
    /// <summary>
    /// Gets a agent by ID or alias.
    /// </summary>
    /// <param name="service">The agent service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent if found and the current user may use it, otherwise null.</returns>
    public static Task<Core.Agents.AIAgent?> GetAgentAsync(
        this IAIAgentService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
        => GetAgentAsync(service, idOrAlias, includeRestricted: false, cancellationToken);

    /// <summary>
    /// Gets a agent by ID or alias, optionally including agents the current user may not use.
    /// </summary>
    /// <param name="service">The agent service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="includeRestricted">Ask for managed access. Only honoured with the Manage agents right.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent if found and visible to the caller, otherwise null.</returns>
    public static async Task<Core.Agents.AIAgent?> GetAgentAsync(
        this IAIAgentService service,
        IdOrAlias idOrAlias,
        bool includeRestricted,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return await service.GetAgentAsync(idOrAlias.Id.Value, includeRestricted, cancellationToken);
        }

        if (idOrAlias.Alias != null)
        {
            return await service.GetAgentByAliasAsync(idOrAlias.Alias, includeRestricted, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Gets a agent by ID or alias as seen by the given user groups instead of the current user's.
    /// </summary>
    /// <param name="service">The agent service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="userGroupIds">The user group keys to check the agent's limit against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent if found and usable by those groups, otherwise null.</returns>
    public static async Task<Core.Agents.AIAgent?> GetAgentAsync(
        this IAIAgentService service,
        IdOrAlias idOrAlias,
        IReadOnlyCollection<Guid> userGroupIds,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return await service.GetAgentAsync(idOrAlias.Id.Value, userGroupIds, cancellationToken);
        }

        if (idOrAlias.Alias != null)
        {
            return await service.GetAgentByAliasAsync(idOrAlias.Alias, userGroupIds, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Gets a agent by ID or alias so it can be used, refusing one the current user may not use.
    /// </summary>
    /// <param name="service">The agent service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent when the current user may use it, null when there is no such agent.</returns>
    /// <exception cref="AIAgentNotAvailableException">Thrown if the agent exists but the current user may not use it.</exception>
    public static async Task<Core.Agents.AIAgent?> GetAgentForUseAsync(
        this IAIAgentService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        if (idOrAlias is { IsId: true, Id: not null })
        {
            return await service.GetAgentForUseAsync(idOrAlias.Id.Value, cancellationToken);
        }

        if (idOrAlias.Alias != null)
        {
            return await service.GetAgentForUseByAliasAsync(idOrAlias.Alias, cancellationToken);
        }

        return null;
    }

    /// <summary>
    /// Tries to get the ID of the agent a run endpoint was asked for, refusing one the current user may not use.
    /// </summary>
    /// <param name="service">The agent service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The agent ID. An ID that matches no agent is returned as it is, for the run to report; an alias that
    /// matches no agent gives null.
    /// </returns>
    /// <exception cref="AIAgentNotAvailableException">Thrown if the agent exists but the current user may not use it.</exception>
    public static async Task<Guid?> TryGetAgentIdForUseAsync(
        this IAIAgentService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        var agent = await GetAgentForUseAsync(service, idOrAlias, cancellationToken);
        return agent?.Id ?? idOrAlias.Id;
    }

    /// <summary>
    /// Tries to get a agent ID by ID or alias, including agents the current user may not use when they have the
    /// Manage agents right.
    /// If already an ID, returns it directly without a database lookup.
    /// </summary>
    /// <param name="service">The agent service.</param>
    /// <param name="idOrAlias">The ID or alias to look up.</param>
    /// <param name="includeRestricted">Ask for managed access. Only honoured with the Manage agents right.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent ID if found, otherwise null.</returns>
    public static async Task<Guid?> TryGetAgentIdAsync(
        this IAIAgentService service,
        IdOrAlias idOrAlias,
        bool includeRestricted,
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
            var agent = await service.GetAgentByAliasAsync(idOrAlias.Alias, includeRestricted, cancellationToken);
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
        this IAIAgentService service,
        IdOrAlias idOrAlias,
        CancellationToken cancellationToken = default)
    {
        var agentId = await TryGetAgentIdAsync(service, idOrAlias, includeRestricted: false, cancellationToken);
        if (agentId is null)
        {
            throw new InvalidOperationException(
                $"Unable to find a agent with the id or alias of '{idOrAlias}'");
        }

        return agentId.Value;
    }
}
