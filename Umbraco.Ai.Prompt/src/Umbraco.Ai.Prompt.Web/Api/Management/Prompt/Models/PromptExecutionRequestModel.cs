using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;

/// <summary>
/// Request model for prompt execution.
/// </summary>
public class PromptExecutionRequestModel
{
    /// <summary>
    /// The entity ID for context.
    /// Required for scope validation.
    /// </summary>
    [Required]
    public required Guid EntityId { get; init; }

    /// <summary>
    /// The entity type (e.g., "document", "media").
    /// Required for scope validation.
    /// </summary>
    [Required]
    public required string EntityType { get; init; }

    /// <summary>
    /// The property alias being edited.
    /// Required for scope validation.
    /// </summary>
    [Required]
    public required string PropertyAlias { get; init; }

    /// <summary>
    /// The culture variant.
    /// </summary>
    public string? Culture { get; init; }

    /// <summary>
    /// The segment variant.
    /// </summary>
    public string? Segment { get; init; }

    /// <summary>
    /// Flexible context items array for passing frontend context to processors.
    /// Each item contains a description and optional JSON value.
    /// </summary>
    public IReadOnlyList<RequestContextItemModel>? Context { get; init; }
}
