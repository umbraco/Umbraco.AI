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
    /// The items contributed by this knowledge set, as metadata only. Content is materialised lazily via
    /// the per-item content endpoint, not returned here.
    /// </summary>
    public IEnumerable<KnowledgeSetItemModel> Items { get; set; } = [];
}

/// <summary>
/// Response model for a single item within a knowledge set (metadata only).
/// </summary>
/// <remarks>
/// Content is deliberately omitted: items are no longer materialised for the listing. The markdown body
/// is fetched on demand via the per-item content endpoint keyed on <see cref="Key"/>.
/// </remarks>
public class KnowledgeSetItemModel
{
    /// <summary>
    /// The stable key of the item within its knowledge set, used to fetch its content on demand.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The display name of the item.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The description of the item — the breadcrumb the LLM sees when the item is advertised on demand.
    /// </summary>
    public string? Description { get; set; }
}
