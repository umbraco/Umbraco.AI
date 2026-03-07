namespace Umbraco.AI.Agent.Persistence.Orchestrations;

/// <summary>
/// EF Core entity for orchestration storage.
/// </summary>
internal class AIOrchestrationEntity
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
    /// JSON-serialized array of surface IDs.
    /// </summary>
    public string? SurfaceIds { get; set; }

    /// <summary>
    /// JSON-serialized scope (AllowRules and DenyRules).
    /// Null means available everywhere.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// JSON-serialized orchestration graph (nodes and edges).
    /// </summary>
    public string? Graph { get; set; }

    /// <summary>
    /// Whether the orchestration is active.
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
    /// The key (GUID) of the user who created this orchestration.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// The key (GUID) of the user who last modified this orchestration.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }

    /// <summary>
    /// Current version of the orchestration.
    /// </summary>
    public int Version { get; set; } = 1;
}
