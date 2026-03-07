using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Agent.Web.Api.Management.Orchestration.Models;

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
}
