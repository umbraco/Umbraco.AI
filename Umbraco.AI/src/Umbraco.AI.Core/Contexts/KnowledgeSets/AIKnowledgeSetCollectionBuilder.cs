using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Contexts.KnowledgeSets;

/// <summary>
/// A lazy collection builder for AI knowledge sets.
/// </summary>
/// <remarks>
/// Knowledge sets are auto-discovered via <see cref="IDiscoverable"/> and the <see cref="AIKnowledgeSetAttribute"/>.
/// Use <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add knowledge sets manually,
/// or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to exclude auto-discovered sets.
/// </remarks>
public class AIKnowledgeSetCollectionBuilder
    : LazyCollectionBuilderBase<AIKnowledgeSetCollectionBuilder, AIKnowledgeSetCollection, IAIKnowledgeSet>
{
    /// <inheritdoc />
    protected override AIKnowledgeSetCollectionBuilder This => this;
}
