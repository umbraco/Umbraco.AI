namespace Umbraco.Ai.Agent.Core.Agents;

/// <summary>
/// Represents a stored agent definition that can be linked to AI profiles.
/// </summary>
public sealed class AiAgent
{
    /// <summary>
    /// Unique identifier for the agent.
    /// </summary>
    public Guid Id { get; internal set; }

    /// <summary>
    /// Unique alias for the agent (URL-safe identifier).
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// Display name for the agent.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of what the agent does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Profile to use for AI model configuration.
    /// </summary>
    public required Guid ProfileId { get; set; }

    /// <summary>
    /// Context IDs assigned to this agent for AI context injection.
    /// </summary>
    public IReadOnlyList<Guid> ContextIds { get; set; } = [];

    /// <summary>
    /// Instructions that define how the agent behaves.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Whether this agent is active and available for use.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
