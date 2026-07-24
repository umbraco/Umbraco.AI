namespace Umbraco.AI.Web.Api.Management.KnowledgeSets.Models;

/// <summary>
/// Response model for the materialised content of a single knowledge set item.
/// </summary>
/// <remarks>
/// Content is fetched lazily via the per-item content endpoint, awaiting the item's async producer. It
/// ships in the assembly (not secret), so returning it for audit in the backoffice is fine.
/// </remarks>
public class KnowledgeSetItemContentResponseModel
{
    /// <summary>
    /// The stable key of the item within its knowledge set.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The materialised markdown content of the item.
    /// </summary>
    public required string Content { get; set; }
}
