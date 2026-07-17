namespace Umbraco.AI.Web.Api.Management.KnowledgeSets.Models;

/// <summary>
/// Response model for a knowledge set (list shape).
/// </summary>
public class KnowledgeSetResponseModel
{
    /// <summary>
    /// The unique identifier of the knowledge set (e.g., "umbraco-engage").
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The display name for the knowledge set.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The description of the knowledge set.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The icon alias for the knowledge set.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// The number of items contributed by this knowledge set.
    /// </summary>
    public int ItemCount { get; set; }
}
