namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Represents a stored prompt template that can be linked to AI profiles.
/// </summary>
public class AiPrompt
{
    /// <summary>
    /// Unique identifier for the prompt.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Unique alias for the prompt (URL-safe identifier).
    /// </summary>
    public required string Alias { get; set; }

    /// <summary>
    /// Display name for the prompt.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of what the prompt does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The prompt template content. May include placeholders like {{variable}}.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Optional ID of the AI profile this prompt is designed for.
    /// References AiProfile.Id from Umbraco.Ai.Core.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Tags for categorization and filtering.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>
    /// Whether this prompt is active and available for use.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Scope configuration defining where this prompt appears as a property action.
    /// If null, the prompt does not appear anywhere (scoped by default).
    /// </summary>
    public AiPromptScope? Scope { get; set; }

    /// <summary>
    /// When the prompt was created.
    /// </summary>
    public DateTime DateCreated { get; init; }

    /// <summary>
    /// When the prompt was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }
}
