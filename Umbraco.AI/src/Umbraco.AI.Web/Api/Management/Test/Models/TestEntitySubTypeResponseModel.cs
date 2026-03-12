namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Response model for an entity sub-type (e.g., content type, media type).
/// </summary>
public sealed class TestEntitySubTypeResponseModel
{
    /// <summary>
    /// The alias of the sub-type (e.g., "blogPost").
    /// </summary>
    public required string Alias { get; init; }

    /// <summary>
    /// The display name of the sub-type (e.g., "Blog Post").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The icon for the sub-type.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// A description of the sub-type.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The unique identifier of the sub-type, if available.
    /// </summary>
    public string? Unique { get; init; }
}
