namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// Response model for a agent list item.
/// </summary>
public class AgentItemResponseModel
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
    /// The linked profile ID.
    /// When null, the default chat profile from Settings will be used.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Context IDs for AI context injection.
    /// </summary>
    public IEnumerable<Guid> ContextIds { get; set; } = [];

    /// <summary>
    /// Scope IDs that categorize this agent for specific purposes.
    /// </summary>
    public IEnumerable<string> ScopeIds { get; set; } = [];

    /// <summary>
    /// List of explicitly allowed tool IDs for this agent.
    /// </summary>
    public IEnumerable<string> AllowedToolIds { get; set; } = [];

    /// <summary>
    /// List of allowed tool scope IDs for this agent.
    /// Grants access to all tools within the specified scopes.
    /// </summary>
    public IEnumerable<string> AllowedToolScopeIds { get; set; } = [];

    /// <summary>
    /// Whether the agent is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the context was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the context was created.
    /// </summary>
    public DateTime DateModified { get; set; }
}
