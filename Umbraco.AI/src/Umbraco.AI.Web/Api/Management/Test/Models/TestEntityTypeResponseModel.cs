namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Response model for a registered entity type.
/// </summary>
public sealed class TestEntityTypeResponseModel
{
    /// <summary>
    /// The entity type identifier (e.g., "document", "media").
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// The display name for this entity type (e.g., "Document", "Media").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The icon for this entity type.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Whether this entity type has sub-types (e.g., content types for documents).
    /// </summary>
    public bool HasSubTypes { get; init; }
}
