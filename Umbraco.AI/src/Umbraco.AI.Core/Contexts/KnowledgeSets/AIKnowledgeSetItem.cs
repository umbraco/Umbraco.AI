namespace Umbraco.AI.Core.Contexts.KnowledgeSets;

/// <summary>
/// A single item of knowledge within an <see cref="IAIKnowledgeSet"/>.
/// </summary>
/// <remarks>
/// This is the author-facing surface. It deliberately has no resource type, settings, or injection
/// mode: the author supplies a stable <see cref="Key"/>, a display <see cref="Name"/>, an optional
/// <see cref="Description"/> (the breadcrumb the LLM sees when the item is advertised on demand), and
/// an async <see cref="GetContentAsync"/> producer that materialises the markdown body only when it is
/// actually consumed (when the LLM calls <c>get_context_resource</c> or an admin opens the item). The
/// <see cref="KnowledgeSetContextResolver"/> is the sole place that maps an item into a real context
/// resource, baking in the internal <c>knowledge-content</c> resource type, on-demand injection mode,
/// and a deterministic identifier — so authors never see (or control) those concerns.
/// </remarks>
public sealed class AIKnowledgeSetItem
{
    /// <summary>
    /// The stable identity of the item within its knowledge set. Distinct from <see cref="Name"/>
    /// (which may change), it drives the deterministic resource GUID and the per-item admin API URL,
    /// so it should be stable and URL-safe (e.g. <c>"goals"</c>, <c>"segments"</c>).
    /// </summary>
    public required string Key { get; init; }

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
    /// Asynchronously produces the knowledge content, as markdown, injected as-is when retrieved.
    /// Invoked lazily only when the content is actually consumed — never merely to list the item.
    /// Implementations should honour the supplied <see cref="CancellationToken"/>.
    /// </summary>
    public required Func<CancellationToken, Task<string>> GetContentAsync { get; init; }

    /// <summary>
    /// Convenience factory for the common static case: wraps a literal string so simple sets pay no
    /// async ceremony.
    /// </summary>
    /// <param name="key">The stable, URL-safe identity of the item within its set.</param>
    /// <param name="name">The display name of the item.</param>
    /// <param name="content">The markdown content.</param>
    /// <param name="description">Optional breadcrumb description.</param>
    /// <returns>An <see cref="AIKnowledgeSetItem"/> whose producer returns the literal content.</returns>
    public static AIKnowledgeSetItem FromContent(string key, string name, string content, string? description = null)
        => new()
        {
            Key = key,
            Name = name,
            Description = description,
            GetContentAsync = _ => Task.FromResult(content)
        };
}
