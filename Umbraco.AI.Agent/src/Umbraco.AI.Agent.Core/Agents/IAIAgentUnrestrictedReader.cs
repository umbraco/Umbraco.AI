namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Reads agents without the per-user limit that <see cref="IAIAgentService"/> applies. For server-side code
/// that acts on an agent after the run was already authorized (resolvers, telemetry), or that has no user
/// at all. Never use it on a request path: use <see cref="IAIAgentService"/> there.
/// </summary>
internal interface IAIAgentUnrestrictedReader
{
    /// <summary>
    /// Gets an agent by ID, whoever may use it.
    /// </summary>
    Task<AIAgent?> GetAgentUnrestrictedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all agents, whoever may use them.
    /// </summary>
    Task<IEnumerable<AIAgent>> GetAgentsUnrestrictedAsync(CancellationToken cancellationToken = default);
}
