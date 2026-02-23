namespace Umbraco.AI.Core.EntityAdapter;

/// <summary>
/// Represents a sub-type of an entity type (e.g., a content type for documents).
/// </summary>
public sealed class AIEntitySubType
{
    /// <summary>
    /// Gets the alias of the sub-type (e.g., "blogPost").
    /// </summary>
    public required string Alias { get; init; }

    /// <summary>
    /// Gets the display name of the sub-type (e.g., "Blog Post").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the icon for the sub-type.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets a description of the sub-type.
    /// </summary>
    public string? Description { get; init; }
}
