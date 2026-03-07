namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Strategy for merging results in an <see cref="AIOrchestrationNodeType.Aggregator"/> node.
/// </summary>
public enum AIOrchestrationAggregationStrategy
{
    /// <summary>
    /// Concatenate all outputs sequentially.
    /// </summary>
    Concat,

    /// <summary>
    /// Select the most common result (majority vote).
    /// </summary>
    Vote,

    /// <summary>
    /// Use an LLM to summarize all outputs into a single response.
    /// Uses the orchestration's <see cref="AIOrchestration.ProfileId"/>.
    /// </summary>
    Summarize,

    /// <summary>
    /// Custom aggregation via a registered <c>AITool</c>.
    /// </summary>
    Custom,
}
