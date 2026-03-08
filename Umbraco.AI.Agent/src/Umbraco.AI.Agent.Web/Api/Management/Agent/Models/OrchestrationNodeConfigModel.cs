using System.Text.Json.Serialization;
using Umbraco.AI.Agent.Core.Orchestrations;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// Polymorphic base class for orchestration node type-specific configuration.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(StartNodeConfigModel), "start")]
[JsonDerivedType(typeof(EndNodeConfigModel), "end")]
[JsonDerivedType(typeof(AgentNodeConfigModel), "agent")]
[JsonDerivedType(typeof(ToolCallNodeConfigModel), "toolCall")]
[JsonDerivedType(typeof(RouterNodeConfigModel), "router")]
[JsonDerivedType(typeof(AggregatorNodeConfigModel), "aggregator")]
[JsonDerivedType(typeof(CommunicationBusNodeConfigModel), "communicationBus")]
public abstract class NodeConfigModel;

/// <summary>
/// Config for Start nodes (no configuration needed).
/// </summary>
public sealed class StartNodeConfigModel : NodeConfigModel;

/// <summary>
/// Config for End nodes (no configuration needed).
/// </summary>
public sealed class EndNodeConfigModel : NodeConfigModel;

/// <summary>
/// Config for Agent nodes.
/// </summary>
public sealed class AgentNodeConfigModel : NodeConfigModel
{
    /// <summary>
    /// The ID of the referenced agent.
    /// </summary>
    public Guid? AgentId { get; set; }

    /// <summary>
    /// When true, this agent acts as the manager in a Communication Bus.
    /// </summary>
    public bool IsManager { get; set; }
}

/// <summary>
/// Config for Tool Call nodes.
/// </summary>
public sealed class ToolCallNodeConfigModel : NodeConfigModel
{
    /// <summary>
    /// The ID of a registered IAITool.
    /// </summary>
    public string? ToolId { get; set; }
}

/// <summary>
/// Config for Router nodes. Conditions are defined on outgoing edges.
/// </summary>
public sealed class RouterNodeConfigModel : NodeConfigModel;

/// <summary>
/// Config for Aggregator nodes.
/// </summary>
public sealed class AggregatorNodeConfigModel : NodeConfigModel
{
    /// <summary>
    /// The strategy used to merge concurrent results.
    /// </summary>
    public AIOrchestrationAggregationStrategy AggregationStrategy { get; set; } = AIOrchestrationAggregationStrategy.Concat;

    /// <summary>
    /// Optional profile override for the Summarize strategy.
    /// If null, the orchestrated agent's own profile is used.
    /// </summary>
    public Guid? ProfileId { get; set; }
}

/// <summary>
/// Config for Communication Bus nodes.
/// </summary>
public sealed class CommunicationBusNodeConfigModel : NodeConfigModel
{
    /// <summary>
    /// Maximum number of iterations before the bus terminates. Defaults to 40.
    /// </summary>
    public int MaxIterations { get; set; } = 40;

    /// <summary>
    /// Optional message that signals the bus should terminate.
    /// </summary>
    public string? TerminationMessage { get; set; }
}
