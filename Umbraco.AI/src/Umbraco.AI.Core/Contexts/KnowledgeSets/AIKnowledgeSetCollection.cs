using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Contexts.KnowledgeSets;

/// <summary>
/// A collection of AI knowledge sets.
/// </summary>
public sealed class AIKnowledgeSetCollection : BuilderCollectionBase<IAIKnowledgeSet>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIKnowledgeSetCollection"/> class.
    /// </summary>
    /// <param name="items">A factory function that returns the knowledge sets.</param>
    public AIKnowledgeSetCollection(Func<IEnumerable<IAIKnowledgeSet>> items)
        : base(items)
    { }

    /// <summary>
    /// Gets a knowledge set by its unique identifier.
    /// </summary>
    /// <param name="id">The knowledge set identifier (e.g., "umbraco-engage").</param>
    /// <returns>The knowledge set, or <c>null</c> if not found.</returns>
    public IAIKnowledgeSet? GetById(string id)
        => this.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
