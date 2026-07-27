namespace Umbraco.AI.Core.Contexts.KnowledgeSets;

/// <summary>
/// The settings carried by a knowledge-set context resource. It is a <em>reference</em> to a knowledge
/// item — the knowledge set id plus the item key — and never the content itself. The Core-internal
/// <see cref="KnowledgeContentResourceType"/> uses this pair to re-locate the item and await its
/// <see cref="AIKnowledgeSetItem.GetContentAsync"/> at format time, keeping content lazy.
/// </summary>
public sealed class KnowledgeContentRef
{
    /// <summary>
    /// The id of the knowledge set the referenced item belongs to.
    /// </summary>
    public required string KnowledgeSetId { get; init; }

    /// <summary>
    /// The stable key of the referenced item within its knowledge set.
    /// </summary>
    public required string ItemKey { get; init; }
}
