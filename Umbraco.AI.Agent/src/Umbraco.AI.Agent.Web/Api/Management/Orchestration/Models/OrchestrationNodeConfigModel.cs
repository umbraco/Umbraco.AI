using Umbraco.AI.Agent.Core.Orchestrations;

namespace Umbraco.AI.Agent.Web.Api.Management.Orchestration.Models;

/// <summary>
/// API model for type-specific node configuration.
/// Only the properties relevant to the node's type are used.
/// </summary>
public class OrchestrationNodeConfigModel
{
    /// <summary>
    /// For Agent nodes: the ID of the referenced agent.
    /// </summary>
    public Guid? AgentId { get; set; }

    /// <summary>
    /// For Function nodes: the name of a registered AITool.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// For Router nodes: structured conditions that determine routing.
    /// </summary>
    public IList<OrchestrationRouteConditionModel>? Conditions { get; set; }

    /// <summary>
    /// For Aggregator nodes: the strategy used to merge concurrent results.
    /// </summary>
    public AIOrchestrationAggregationStrategy? AggregationStrategy { get; set; }

    /// <summary>
    /// For Manager nodes: instructions that tell the manager how to delegate work.
    /// </summary>
    public string? ManagerInstructions { get; set; }

    /// <summary>
    /// For Manager nodes: the profile to use for the manager's LLM calls.
    /// </summary>
    public Guid? ManagerProfileId { get; set; }
}
