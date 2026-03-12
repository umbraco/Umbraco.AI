namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Defines the type of an AI agent, determining its configuration shape and behavior.
/// </summary>
public enum AIAgentType
{
    /// <summary>
    /// A standard agent with instructions, context injection, and tool permissions.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// An orchestrated agent that composes multiple agents into a workflow graph.
    /// </summary>
    Orchestrated = 1,
}
