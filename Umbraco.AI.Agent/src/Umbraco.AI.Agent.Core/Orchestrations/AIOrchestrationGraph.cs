namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Represents the workflow graph definition for an orchestration.
/// Stored as JSON in the database.
/// </summary>
public sealed class AIOrchestrationGraph
{
    /// <summary>
    /// The nodes in the graph (agents, functions, routers, etc.).
    /// </summary>
    public IList<AIOrchestrationNode> Nodes { get; set; } = [];

    /// <summary>
    /// The edges connecting nodes in the graph.
    /// </summary>
    public IList<AIOrchestrationEdge> Edges { get; set; } = [];
}
