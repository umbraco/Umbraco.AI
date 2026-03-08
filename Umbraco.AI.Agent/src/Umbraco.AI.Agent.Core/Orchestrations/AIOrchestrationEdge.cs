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
    /// When no condition matches, the default edge is followed.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Evaluation order for conditional edges (lower priority is evaluated first).
    /// </summary>
    public int? Priority { get; set; }

    /// <summary>
    /// For edges leaving a <see cref="AIOrchestrationNodeType.Router"/> node:
    /// the condition that must be met for this edge to be followed.
    /// If <c>null</c> on a router edge, it acts as the default/fallback route.
    /// </summary>
    public AIOrchestrationRouteCondition? Condition { get; set; }

    /// <summary>
    /// Whether traversing this edge requires human approval before continuing.
    /// When <c>true</c>, execution pauses and waits for an approval decision.
    /// </summary>
    public bool RequiresApproval { get; set; }
}
