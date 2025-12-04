namespace Umbraco.Ai.Prompt.Persistence.Prompts;

/// <summary>
/// EF Core entity for prompt storage.
/// </summary>
public class AiPromptEntity
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
    /// AiPrompt template content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional linked profile ID (soft FK).
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// JSON-serialized tags array.
    /// </summary>
    public string? TagsJson { get; set; }

    /// <summary>
    /// Whether the prompt is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// JSON-serialized scope configuration.
    /// </summary>
    public string? ScopeJson { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime DateModified { get; set; }
}
