using Umbraco.AI.Agent.Core.Orchestrations;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Configuration for an orchestrated agent that composes multiple agents into a workflow graph.
/// </summary>
public sealed class AIOrchestratedAgentConfig : IAIAgentConfig
{
    /// <summary>
    /// The workflow graph definition containing nodes and edges.
    /// </summary>
    public AIOrchestrationGraph Graph { get; set; } = new();
}
