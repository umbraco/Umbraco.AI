using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// API model for a directed edge connecting two nodes in the graph.
/// </summary>
public class OrchestrationEdgeModel
{
    /// <summary>
    /// Unique identifier for the edge within the graph.
    /// </summary>
    [Required]
    public required string Id { get; set; }

    /// <summary>
    /// The ID of the source node.
    /// </summary>
    [Required]
    public required string SourceNodeId { get; set; }

    /// <summary>
    /// The ID of the target node.
    /// </summary>
    [Required]
    public required string TargetNodeId { get; set; }

    /// <summary>
    /// Whether this is the default/fallback edge for a router node.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Evaluation order for conditional edges (lower priority is evaluated first).
    /// </summary>
    public int? Priority { get; set; }

    /// <summary>
    /// For edges leaving a Router node: the condition for this route.
    /// </summary>
    public OrchestrationRouteConditionModel? Condition { get; set; }

    /// <summary>
    /// Optional handle ID on the source node (e.g. "top-source", "left-source").
    /// Used to restore edge positions for nodes with multiple handles.
    /// </summary>
    public string? SourceHandle { get; set; }

    /// <summary>
    /// Optional handle ID on the target node (e.g. "bottom-target", "right-target").
    /// Used to restore edge positions for nodes with multiple handles.
    /// </summary>
    public string? TargetHandle { get; set; }

    /// <summary>
    /// Whether traversing this edge requires human approval before continuing.
    /// </summary>
    public bool RequiresApproval { get; set; }
}
