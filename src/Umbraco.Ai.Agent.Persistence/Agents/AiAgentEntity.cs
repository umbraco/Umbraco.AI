namespace Umbraco.Ai.Agent.Persistence.Agents;

/// <summary>
/// EF Core entity for prompt storage.
/// </summary>
public class AiAgentEntity
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
    /// AiAgent template content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional linked profile ID (soft FK).
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Tags array serialized as a comma-separated string.
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Whether the prompt is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// JSON-serialized scope configuration.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime DateModified { get; set; }
}
