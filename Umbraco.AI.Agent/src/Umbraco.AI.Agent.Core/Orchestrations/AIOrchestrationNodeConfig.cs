namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Type-specific configuration for an orchestration node.
/// Only the properties relevant to the node's <see cref="AIOrchestrationNodeType"/> are used.
/// </summary>
public sealed class AIOrchestrationNodeConfig
{
    /// <summary>
    /// For <see cref="AIOrchestrationNodeType.Agent"/> nodes:
    /// the ID of the referenced <see cref="Agents.AIAgent"/>.
    /// </summary>
    public Guid? AgentId { get; set; }

    /// <summary>
    /// For <see cref="AIOrchestrationNodeType.Function"/> nodes:
    /// the name of a registered M.E.AI <c>AITool</c>.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// For <see cref="AIOrchestrationNodeType.Router"/> nodes:
    /// structured conditions that determine routing.
    /// </summary>
    public IList<AIOrchestrationRouteCondition>? Conditions { get; set; }

    /// <summary>
    /// For <see cref="AIOrchestrationNodeType.Aggregator"/> nodes:
    /// the strategy used to merge concurrent results.
    /// </summary>
    public AIOrchestrationAggregationStrategy? AggregationStrategy { get; set; }

    /// <summary>
    /// For <see cref="AIOrchestrationNodeType.Manager"/> nodes:
    /// instructions that tell the manager how to delegate work to other agents.
    /// </summary>
    public string? ManagerInstructions { get; set; }

    /// <summary>
    /// For <see cref="AIOrchestrationNodeType.Manager"/> nodes:
    /// the profile to use for the manager's LLM calls.
    /// </summary>
    public Guid? ManagerProfileId { get; set; }
}
