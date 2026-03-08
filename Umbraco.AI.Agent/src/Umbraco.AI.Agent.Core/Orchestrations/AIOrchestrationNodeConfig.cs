using System.Text.Json.Serialization;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Marker interface for orchestration node configuration.
/// Each <see cref="AIOrchestrationNodeType"/> has its own concrete config class.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(AIOrchestrationStartNodeConfig), "start")]
[JsonDerivedType(typeof(AIOrchestrationEndNodeConfig), "end")]
[JsonDerivedType(typeof(AIOrchestrationAgentNodeConfig), "agent")]
[JsonDerivedType(typeof(AIOrchestrationToolCallNodeConfig), "toolCall")]
[JsonDerivedType(typeof(AIOrchestrationRouterNodeConfig), "router")]
[JsonDerivedType(typeof(AIOrchestrationAggregatorNodeConfig), "aggregator")]
[JsonDerivedType(typeof(AIOrchestrationCommunicationBusNodeConfig), "communicationBus")]
public interface IAIOrchestrationNodeConfig;

/// <summary>
/// Empty config for <see cref="AIOrchestrationNodeType.Start"/> nodes.
/// </summary>
public sealed class AIOrchestrationStartNodeConfig : IAIOrchestrationNodeConfig;

/// <summary>
/// Empty config for <see cref="AIOrchestrationNodeType.End"/> nodes.
/// </summary>
public sealed class AIOrchestrationEndNodeConfig : IAIOrchestrationNodeConfig;

/// <summary>
/// Config for <see cref="AIOrchestrationNodeType.Agent"/> nodes.
/// </summary>
public sealed class AIOrchestrationAgentNodeConfig : IAIOrchestrationNodeConfig
{
    /// <summary>
    /// The ID of the referenced <see cref="Agents.AIAgent"/>.
    /// </summary>
    public Guid? AgentId { get; set; }

    /// <summary>
    /// When <c>true</c>, this agent acts as the manager in a Communication Bus
    /// (controls speaker selection in group chat, or coordinates handoff routing).
    /// </summary>
    public bool IsManager { get; set; }
}

/// <summary>
/// Config for <see cref="AIOrchestrationNodeType.ToolCall"/> nodes.
/// References a registered <see cref="Umbraco.AI.Core.Tools.IAITool"/> by its ID.
/// </summary>
public sealed class AIOrchestrationToolCallNodeConfig : IAIOrchestrationNodeConfig
{
    /// <summary>
    /// The ID of a registered <see cref="Umbraco.AI.Core.Tools.IAITool"/>.
    /// </summary>
    public string? ToolId { get; set; }
}

/// <summary>
/// Config for <see cref="AIOrchestrationNodeType.Router"/> nodes.
/// Conditions are defined on outgoing edges, so this config only holds the label.
/// </summary>
public sealed class AIOrchestrationRouterNodeConfig : IAIOrchestrationNodeConfig;

/// <summary>
/// Config for <see cref="AIOrchestrationNodeType.Aggregator"/> nodes.
/// </summary>
public sealed class AIOrchestrationAggregatorNodeConfig : IAIOrchestrationNodeConfig
{
    /// <summary>
    /// The strategy used to merge concurrent results.
    /// </summary>
    public AIOrchestrationAggregationStrategy AggregationStrategy { get; set; } = AIOrchestrationAggregationStrategy.Concat;

    /// <summary>
    /// Optional profile override for the <see cref="AIOrchestrationAggregationStrategy.Summarize"/> strategy.
    /// If <c>null</c>, the orchestrated agent's own profile is used.
    /// </summary>
    public Guid? ProfileId { get; set; }
}

/// <summary>
/// Config for <see cref="AIOrchestrationNodeType.CommunicationBus"/> nodes.
/// Represents a shared agent collaboration space that maps to MAF GroupChat or Handoff.
/// </summary>
public sealed class AIOrchestrationCommunicationBusNodeConfig : IAIOrchestrationNodeConfig
{
    /// <summary>
    /// Maximum number of iterations before the bus terminates. Defaults to 40 (MAF default).
    /// </summary>
    public int MaxIterations { get; set; } = 40;

    /// <summary>
    /// Optional message that signals the bus should terminate.
    /// </summary>
    public string? TerminationMessage { get; set; }
}
