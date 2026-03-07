using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Represents a single step in the orchestration execution pipeline.
/// </summary>
internal sealed record OrchestrationStep(
    string NodeId,
    string Label,
    OrchestrationStepType StepType,
    MsAIAgent? Agent = null,
    string? ToolName = null,
    IList<AIOrchestrationRouteCondition>? Conditions = null,
    IList<AIOrchestrationEdge>? SuccessorEdges = null,
    AIOrchestrationAggregationStrategy AggregationStrategy = AIOrchestrationAggregationStrategy.Concat,
    string? ManagerInstructions = null);

/// <summary>
/// The type of orchestration execution step.
/// </summary>
internal enum OrchestrationStepType
{
    Agent,
    Function,
    Router,
    Aggregator,
    Manager,
    End,
}
