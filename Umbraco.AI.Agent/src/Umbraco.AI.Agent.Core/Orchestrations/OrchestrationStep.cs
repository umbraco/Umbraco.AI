using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Base for all orchestration execution steps.
/// </summary>
internal abstract record OrchestrationStep(string NodeId, string Label);

/// <summary>
/// Executes a referenced agent.
/// </summary>
internal sealed record AgentOrchestrationStep(
    string NodeId,
    string Label,
    MsAIAgent Agent,
    bool IsManager = false) : OrchestrationStep(NodeId, Label);

/// <summary>
/// Executes a registered <see cref="Umbraco.AI.Core.Tools.IAITool"/> without an LLM.
/// </summary>
internal sealed record ToolCallOrchestrationStep(
    string NodeId,
    string Label,
    string ToolId) : OrchestrationStep(NodeId, Label);

/// <summary>
/// Evaluates edge conditions to determine the next step.
/// </summary>
internal sealed record RouterOrchestrationStep(
    string NodeId,
    string Label,
    IReadOnlyList<AIOrchestrationEdge> OutgoingEdges) : OrchestrationStep(NodeId, Label);

/// <summary>
/// Merges results from concurrent branches.
/// </summary>
internal sealed record AggregatorOrchestrationStep(
    string NodeId,
    string Label,
    AIOrchestrationAggregationStrategy Strategy,
    Guid? ProfileId = null) : OrchestrationStep(NodeId, Label);

/// <summary>
/// Shared agent collaboration space (maps to MAF GroupChat or Handoff).
/// </summary>
internal sealed record CommunicationBusOrchestrationStep(
    string NodeId,
    string Label,
    IReadOnlyList<MsAIAgent> Participants,
    MsAIAgent? Manager,
    int MaxIterations = 40,
    string? TerminationMessage = null) : OrchestrationStep(NodeId, Label);

/// <summary>
/// Terminal step — workflow exit point.
/// </summary>
internal sealed record EndOrchestrationStep(
    string NodeId,
    string Label) : OrchestrationStep(NodeId, Label);
