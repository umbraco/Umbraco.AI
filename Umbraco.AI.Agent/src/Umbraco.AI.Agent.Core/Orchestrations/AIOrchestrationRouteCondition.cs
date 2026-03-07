namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// A structured condition for routing in a <see cref="AIOrchestrationNodeType.Router"/> node.
/// Conditions are evaluated against the previous node's output metadata.
/// </summary>
public sealed class AIOrchestrationRouteCondition
{
    /// <summary>
    /// Display label for this condition on the edge (e.g., "Billing query").
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// Output field to evaluate (e.g., "category", "sentiment").
    /// </summary>
    public required string Field { get; set; }

    /// <summary>
    /// The comparison operator.
    /// </summary>
    public required AIOrchestrationRouteOperator Operator { get; set; }

    /// <summary>
    /// The expected value to compare against.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// The target node ID to route to when this condition matches.
    /// </summary>
    public required string TargetNodeId { get; set; }
}
