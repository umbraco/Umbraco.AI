namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// Response model for a agent.
/// </summary>
public class AgentResponseModel
{
    /// <summary>
    /// The unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The unique alias.
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The agent content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional linked profile ID.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public IEnumerable<string> Tags { get; set; } = [];

    /// <summary>
    /// Whether the agent is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Scope configuration defining where this agent can run.
    /// Null means the agent is not allowed anywhere.
    /// </summary>
    public ScopeModel? Scope { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime DateModified { get; set; }
}
