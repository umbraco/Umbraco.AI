using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Translates a stored orchestration graph into a MAF workflow agent.
/// </summary>
/// <remarks>
/// <para>
/// The executor reads the orchestration graph, creates MAF agents for each Agent node
/// (via <see cref="Chat.IAIAgentFactory"/>), resolves AITools for Function nodes,
/// and wires everything together via MAF's <c>WorkflowBuilder</c>.
/// </para>
/// <para>
/// The resulting agent can be used with MAF's <c>RunAsync</c> and <c>RunStreamingAsync</c>
/// methods, or exposed as a surface-level agent.
/// </para>
/// </remarks>
public interface IAIOrchestrationExecutor
{
    /// <summary>
    /// Builds a MAF workflow agent from the stored orchestration graph.
    /// </summary>
    /// <param name="orchestration">The orchestration containing the workflow graph.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A MAF <see cref="MsAIAgent"/> representing the workflow, ready for execution.</returns>
    Task<MsAIAgent> BuildWorkflowAgentAsync(
        AIOrchestration orchestration,
        CancellationToken cancellationToken = default);
}
