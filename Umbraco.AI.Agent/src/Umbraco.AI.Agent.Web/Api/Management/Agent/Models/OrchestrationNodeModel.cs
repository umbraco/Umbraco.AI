using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Agent.Core.Orchestrations;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// API model for a node in the orchestration workflow graph.
/// </summary>
public class OrchestrationNodeModel
{
    /// <summary>
    /// Unique identifier within the graph (e.g., "node-1").
    /// </summary>
    [Required]
    public required string Id { get; set; }

    /// <summary>
    /// The type of node (agent, function, router, etc.).
    /// </summary>
    [Required]
    public required AIOrchestrationNodeType Type { get; set; }

    /// <summary>
    /// Display label for the node in the visual editor.
    /// </summary>
    [Required]
    public required string Label { get; set; }

    /// <summary>
    /// X position in the visual editor (persisted for layout).
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y position in the visual editor (persisted for layout).
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Type-specific configuration for the node.
    /// </summary>
    public required NodeConfigModel Config { get; set; }
}
