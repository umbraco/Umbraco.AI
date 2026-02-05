namespace Umbraco.AI.Agent.Persistence.Agents;

/// <summary>
/// EF Core entity for agent storage.
/// </summary>
internal class AIAgentEntity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique alias (URL-safe identifier).
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional linked profile ID (soft FK).
    /// When null, the default chat profile from Settings will be used.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// JSON-serialized array of context IDs.
    /// </summary>
    public string? ContextIds { get; set; }

    /// <summary>
    /// JSON-serialized array of scope IDs.
    /// </summary>
    public string? ScopeIds { get; set; }

    /// <summary>
    /// JSON-serialized array of enabled tool IDs.
    /// </summary>
    public string? AllowedToolIds { get; set; }

    /// <summary>
    /// JSON-serialized array of enabled tool scope IDs.
    /// </summary>
    public string? AllowedToolScopeIds { get; set; }

    /// <summary>
    /// Instructions that define how the agent behaves.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Whether the agent is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The key (GUID) of the user who created this agent.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// The key (GUID) of the user who last modified this agent.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }

    /// <summary>
    /// Current version of the agent.
    /// </summary>
    public int Version { get; set; } = 1;
}
