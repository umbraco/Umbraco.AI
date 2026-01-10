using Umbraco.Ai.Core.RequestContext;

namespace Umbraco.Ai.Prompt.Core.Prompts;

/// <summary>
/// Request model for prompt execution.
/// </summary>
public class AiPromptExecutionRequest
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
    /// The culture/language variant.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The segment variant.
    /// </summary>
    public string? Segment { get; init; }

    /// <summary>
    /// Flexible context items array for passing frontend context to processors.
    /// These items are processed by <see cref="AiRequestContextProcessorCollection"/>
    /// to extract entity data, template variables, and system messages.
    /// </summary>
    public IReadOnlyList<AiRequestContextItem>? Context { get; init; }
}
