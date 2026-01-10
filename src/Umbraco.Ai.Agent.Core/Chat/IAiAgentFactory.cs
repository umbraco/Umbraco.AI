using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Agent.Core.Agents;

namespace Umbraco.Ai.Agent.Core.Chat;

/// <summary>
/// Factory for creating MAF AIAgent instances from agent definitions.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates <see cref="ChatClientAgent"/> instances that wrap the Umbraco.Ai
/// chat pipeline. The created agents can be used directly with MAF's <c>RunAsync</c> and
/// <c>RunStreamingAsync</c> methods for both automation scenarios and AG-UI streaming.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// // Get agent definition
/// var agentDef = await _agentService.GetAgentAsync(agentId, ct);
///
/// // Create MAF agent
/// var agent = await _agentFactory.CreateAgentAsync(agentDef, ct);
///
/// // Use MAF API
/// var response = await agent.RunAsync("Do something");
/// // or streaming
/// await foreach (var update in agent.RunStreamingAsync("Do something")) { }
/// </code>
/// </remarks>
public interface IAiAgentFactory
{
    /// <summary>
    /// Creates a MAF AIAgent for the given agent definition.
    /// </summary>
    /// <param name="agent">The agent definition containing instructions and context configuration.</param>
    /// <param name="additionalTools">Optional additional tools to include in the agent.
    ///  Primarily used for frontend tool injection in AG-UI scenarios.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured <see cref="ChatClientAgent"/> with tools and context injection.</returns>
    Task<AIAgent> CreateAgentAsync(
        AiAgent agent,
        IEnumerable<AITool>? additionalTools = null,
        CancellationToken cancellationToken = default);
}
