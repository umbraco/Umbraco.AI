namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Request model for prompt execution.
/// </summary>
public class AiPromptExecutionRequest
{
    /// <summary>
    /// The entity ID (document, media, etc.) for context.
    /// </summary>
    public Guid? EntityId { get; init; }

    /// <summary>
    /// The entity type (e.g., "document", "media").
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// The property alias being edited.
    /// </summary>
    public string? PropertyAlias { get; init; }

    /// <summary>
    /// The culture/language variant.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The segment variant.
    /// </summary>
    public string? Segment { get; init; }

    /// <summary>
    /// Local content model for snapshot creation (future use).
    /// Key = property alias, Value = property value.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? LocalContent { get; init; }

    /// <summary>
    /// Additional context variables for template replacement.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Context { get; init; }
}
