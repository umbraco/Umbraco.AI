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
    /// The agent type (0 = Standard, 1 = Orchestrated).
    /// </summary>
    public int AgentType { get; set; }

    /// <summary>
    /// JSON-serialized type-specific configuration blob.
    /// </summary>
    public string? Config { get; set; }

    /// <summary>
    /// Optional linked profile ID (soft FK).
    /// When null, the default chat profile from Settings will be used.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// JSON-serialized array of guardrail IDs.
    /// </summary>
    public string? GuardrailIds { get; set; }

    /// <summary>
    /// JSON-serialized array of surface IDs.
    /// </summary>
    public string? SurfaceIds { get; set; }

    /// <summary>
    /// JSON-serialized scope (AllowRules and DenyRules).
    /// Defines where the agent is available (section, entity type, workspace).
    /// Null means available everywhere (backwards compatible).
    /// </summary>
    public string? Scope { get; set; }

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
