using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Request model for prompt execution.
/// </summary>
public class AIPromptExecutionRequest
{
    /// <summary>
    /// The entity ID (document, media, etc.) for context.
    /// Required for scope validation.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// The entity type (e.g., "document", "media").
    /// Required for scope validation.
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// The property alias being edited.
    /// Required for scope validation.
    /// </summary>
    public required string PropertyAlias { get; init; }

    /// <summary>
    /// The content type alias for scope validation.
    /// For documents/media, this is the content type alias.
    /// For blocks, this is the element type alias.
    /// </summary>
    public required string ContentTypeAlias { get; init; }

    /// <summary>
    /// The element ID when editing a block element within an entity.
    /// Null when editing the entity directly (e.g., a document property).
    /// When set, <see cref="EntityId"/> refers to the parent entity (document)
    /// and this refers to the block content key.
    /// </summary>
    public Guid? ElementId { get; init; }

    /// <summary>
    /// The element type when editing a block element within an entity.
    /// Null when editing the entity directly.
    /// Example: "block" when editing a block element.
    /// </summary>
    public string? ElementType { get; init; }

    /// <summary>
    /// The culture/language variant.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The segment variant.
    /// </summary>
    public string? Segment { get; init; }

    /// <summary>
    /// Flexible context items array for passing frontend context to contributor.
    /// These items are processed by <see cref="AIRuntimeContextContributorCollection"/>
    /// to extract entity data, template variables, and system messages.
    /// </summary>
    public IReadOnlyList<AIRequestContextItem>? Context { get; init; }
}
