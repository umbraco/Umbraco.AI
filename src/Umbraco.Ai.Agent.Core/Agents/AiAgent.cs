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
    /// The agent definition content. May include placeholders like {{variable}}.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Optional ID of the AI profile this agent is designed for.
    /// References AiProfile.Id from Umbraco.Ai.Core.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Tags for categorization and filtering.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>
    /// Whether this agent is active and available for use.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Scope configuration defining where this agent can run.
    /// Controls both UI display and server-side enforcement.
    /// If null, the agent is not allowed anywhere (denied by default).
    /// </summary>
    public AiAgentScope? Scope { get; set; }

    /// <summary>
    /// When the agent was created.
    /// </summary>
    public DateTime DateCreated { get; init; }

    /// <summary>
    /// When the agent was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }
}
