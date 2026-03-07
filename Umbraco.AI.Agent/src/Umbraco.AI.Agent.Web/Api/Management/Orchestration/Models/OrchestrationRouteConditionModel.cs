using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Agent.Core.Orchestrations;

namespace Umbraco.AI.Agent.Web.Api.Management.Orchestration.Models;

/// <summary>
/// API model for a routing condition in a Router node.
/// </summary>
public class OrchestrationRouteConditionModel
{
    /// <summary>
    /// Display label for this condition (e.g., "Billing query").
    /// </summary>
    [Required]
    public required string Label { get; set; }

    /// <summary>
    /// Output field to evaluate (e.g., "category", "sentiment").
    /// </summary>
    [Required]
    public required string Field { get; set; }

    /// <summary>
    /// The comparison operator.
    /// </summary>
    [Required]
    public required AIOrchestrationRouteOperator Operator { get; set; }

    /// <summary>
    /// The expected value to compare against.
    /// </summary>
    [Required]
    public required string Value { get; set; }

    /// <summary>
    /// The target node ID to route to when this condition matches.
    /// </summary>
    [Required]
    public required string TargetNodeId { get; set; }
}
