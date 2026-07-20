namespace Umbraco.AI.Web.Api.Management.KnowledgeSets.Models;

/// <summary>
/// Response model for a knowledge set including its items (detail shape).
/// </summary>
public class KnowledgeSetDetailResponseModel
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
    /// The items contributed by this knowledge set, including their full content.
    /// </summary>
    public IEnumerable<KnowledgeSetItemModel> Items { get; set; } = [];
}

/// <summary>
/// Response model for a single item within a knowledge set.
/// </summary>
/// <remarks>
/// Full <see cref="Content"/> is returned inline: knowledge-set content ships in the assembly (it is not
/// secret) and is exposed so an admin can audit exactly what the LLM can see.
/// </remarks>
public class KnowledgeSetItemModel
{
    /// <summary>
    /// The display name of the item.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The description of the item — the breadcrumb the LLM sees when the item is advertised on demand.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The knowledge content, as markdown.
    /// </summary>
    public required string Content { get; set; }
}
