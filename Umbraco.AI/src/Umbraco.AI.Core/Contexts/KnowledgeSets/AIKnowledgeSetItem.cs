namespace Umbraco.AI.Core.Contexts.KnowledgeSets;

/// <summary>
/// A single item of knowledge within an <see cref="IAIKnowledgeSet"/>.
/// </summary>
/// <remarks>
/// This is the author-facing surface. It deliberately has no resource type, id, settings, or
/// injection mode: the author only supplies a name, an optional description (the breadcrumb the LLM
/// sees when the item is advertised on demand), and ready-to-inject markdown content. The
/// <see cref="KnowledgeSetContextResolver"/> is the sole place that maps an item into a real
/// context resource, baking in the text resource type, on-demand injection mode, and a deterministic
/// identifier — so authors never see (or control) those concerns.
/// </remarks>
public sealed class AIKnowledgeSetItem
{
    /// <summary>
    /// The display name of the item. Should be unique within its knowledge set.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description of what this item contains. This is the breadcrumb the LLM sees when the
    /// item is advertised on demand, so it should help the model decide whether to retrieve it.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The knowledge content, as markdown, injected as-is when retrieved by the LLM.
    /// </summary>
    public required string Content { get; init; }
}
