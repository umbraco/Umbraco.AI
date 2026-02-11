namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

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
    /// Optional context scope defining where this agent is available.
    /// If null, agent is available in all contexts (backwards compatible).
    /// </summary>
    public AIAgentContextScopeModel? ContextScope { get; set; }

    /// <summary>
    /// Allowed tool IDs for this agent.
    /// Tools must be explicitly allowed or belong to an allowed scope.
    /// System tools are always allowed.
    /// </summary>
    public IEnumerable<string> AllowedToolIds { get; set; } = [];

    /// <summary>
    /// Allowed tool scope IDs for this agent.
    /// Tools belonging to these scopes are automatically allowed.
    /// </summary>
    public IEnumerable<string> AllowedToolScopeIds { get; set; } = [];

    /// <summary>
    /// User group-specific permission overrides.
    /// Dictionary key is UserGroupId (Guid).
    /// </summary>
    public Dictionary<Guid, AIAgentUserGroupPermissionsModel> UserGroupPermissions { get; set; } = [];

    /// <summary>
    /// Instructions that define how the agent behaves.
    /// </summary>
    public string? Instructions { get; set; }

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

    /// <summary>
    /// The current version number of the entity.
    /// </summary>
    public int Version { get; set; }
}
