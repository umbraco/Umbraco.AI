namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// The type of node in an orchestration workflow graph.
/// </summary>
public enum AIOrchestrationNodeType
{
    /// <summary>
    /// Entry point of the workflow (exactly one per graph).
    /// </summary>
    Start,

    /// <summary>
    /// Exit point of the workflow (one or more per graph).
    /// </summary>
    End,

    /// <summary>
    /// References an existing <see cref="Agents.AIAgent"/> by ID.
    /// </summary>
    Agent,

    /// <summary>
    /// A registered M.E.AI <c>AITool</c> that executes without an LLM.
    /// Used for data transformation, API calls, or aggregation logic.
    /// </summary>
    Function,

    /// <summary>
    /// Conditional routing node (rule-based switch).
    /// Evaluates structured conditions to determine the next node.
    /// </summary>
    Router,

    /// <summary>
    /// Merges results from concurrent execution branches.
    /// </summary>
    Aggregator,

    /// <summary>
    /// Magentic pattern manager that dynamically delegates to other agents.
    /// </summary>
    Manager,
}
