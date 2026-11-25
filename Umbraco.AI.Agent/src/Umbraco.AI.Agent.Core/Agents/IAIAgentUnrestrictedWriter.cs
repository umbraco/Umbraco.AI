namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Saves agents without the user group change rule that <see cref="IAIAgentService"/> applies to the signed-in
/// user. For Deploy import only: an import always saves the exported user groups and tool rules, whoever started
/// it. Every other validation still runs. Never use it on a request path: use <see cref="IAIAgentService"/> there.
/// </summary>
internal interface IAIAgentUnrestrictedWriter
{
    /// <summary>
    /// Saves an agent, skipping the check on who may change its user groups and tool rules.
    /// </summary>
    Task<AIAgent> SaveAgentUnrestrictedAsync(AIAgent agent, CancellationToken cancellationToken = default);
}
