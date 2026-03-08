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
    /// When <see cref="AIOrchestrationNodeConfig.IsManager"/> is <c>true</c>, the agent acts
    /// as the group chat manager in a Communication Bus.
    /// </summary>
    Agent,

    /// <summary>
    /// Executes a registered <see cref="Umbraco.AI.Core.Tools.IAITool"/> without an LLM.
    /// Used for data transformation, API calls, or aggregation logic.
    /// </summary>
    ToolCall,

    /// <summary>
    /// Conditional routing node (rule-based switch).
    /// Conditions are defined on outgoing edges, not on the node itself.
    /// </summary>
    Router,

    /// <summary>
    /// Merges results from concurrent execution branches.
    /// </summary>
    Aggregator,

    /// <summary>
    /// Shared agent collaboration space. All agents connected to this node
    /// can communicate with each other. If a connected agent has <see cref="AIOrchestrationNodeConfig.IsManager"/>
    /// set, the pattern maps to MAF GroupChat; otherwise, it maps to MAF Handoff.
    /// </summary>
    CommunicationBus,
}
