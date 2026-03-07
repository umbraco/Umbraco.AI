namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Represents a directed edge connecting two nodes in an orchestration graph.
/// </summary>
public sealed class AIOrchestrationEdge
{
    /// <summary>
    /// Unique identifier for the edge within the graph.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The ID of the source node.
    /// </summary>
    public required string SourceNodeId { get; set; }

    /// <summary>
    /// The ID of the target node.
    /// </summary>
    public required string TargetNodeId { get; set; }

    /// <summary>
    /// Whether this is the default/fallback edge for a router node.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Evaluation order for conditional edges (lower priority is evaluated first).
    /// </summary>
    public int? Priority { get; set; }
}
