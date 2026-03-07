namespace Umbraco.AI.Agent.Core.Orchestrations;

/// <summary>
/// Represents a node in an orchestration workflow graph.
/// </summary>
public sealed class AIOrchestrationNode
{
    /// <summary>
    /// Unique identifier within the graph (e.g., "node-1").
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The type of node (agent, function, router, etc.).
    /// </summary>
    public required AIOrchestrationNodeType Type { get; set; }

    /// <summary>
    /// Display label for the node in the visual editor.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// X position in the visual editor (persisted for layout).
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y position in the visual editor (persisted for layout).
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Type-specific configuration for the node.
    /// </summary>
    public AIOrchestrationNodeConfig Config { get; set; } = new();
}
