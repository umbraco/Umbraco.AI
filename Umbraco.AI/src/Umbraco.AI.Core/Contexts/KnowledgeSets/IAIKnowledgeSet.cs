using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Contexts.KnowledgeSets;

/// <summary>
/// Defines a code-defined, package-embeddable set of background knowledge about a subject
/// (e.g. a product such as "Umbraco Engage") that is surfaced to the LLM.
/// </summary>
/// <remarks>
/// Knowledge sets are the content complement to <see cref="ResourceTypes.IAIContextResourceType"/>:
/// they are discovered at startup via <see cref="IDiscoverable"/> and the
/// <see cref="AIKnowledgeSetAttribute"/>, have no database persistence, and require no server-side
/// registration code. A package author ships knowledge by dropping a single decorated class into
/// their assembly. Every discovered set is auto-active — its items flow to the LLM on demand through
/// the existing context resolution pipeline.
/// Implementations should use the <see cref="AIKnowledgeSetAttribute"/> for auto-discovery, and
/// typically derive from <see cref="AIKnowledgeSetBase"/>.
/// </remarks>
public interface IAIKnowledgeSet : IDiscoverable
{
    /// <summary>
    /// The immutable unique identifier of the knowledge set (e.g., "umbraco-engage").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The display name for the UI.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The description for the UI.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// The Umbraco icon alias for the UI.
    /// </summary>
    string? Icon { get; }

    /// <summary>
    /// Asynchronously produces the items that make up this knowledge set.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The items belonging to this knowledge set.</returns>
    Task<IReadOnlyList<AIKnowledgeSetItem>> GetItemsAsync(CancellationToken cancellationToken = default);
}
