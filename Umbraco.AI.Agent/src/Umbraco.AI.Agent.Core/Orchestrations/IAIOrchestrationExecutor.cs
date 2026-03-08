using Umbraco.AI.Agent.Core.Agents;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Translates an orchestrated agent's graph into a MAF workflow agent.
/// </summary>
public interface IAIOrchestrationExecutor
{
    /// <summary>
    /// Builds a MAF workflow agent from the agent's orchestration graph.
    /// </summary>
    /// <param name="agent">The agent with <see cref="AIOrchestratedAgentConfig"/> containing the workflow graph.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A MAF <see cref="MsAIAgent"/> representing the workflow, ready for execution.</returns>
    /// <exception cref="InvalidOperationException">Thrown when agent is not an orchestrated agent or graph is invalid.</exception>
    Task<MsAIAgent> BuildWorkflowAgentAsync(
        AIAgent agent,
        CancellationToken cancellationToken = default);
}
