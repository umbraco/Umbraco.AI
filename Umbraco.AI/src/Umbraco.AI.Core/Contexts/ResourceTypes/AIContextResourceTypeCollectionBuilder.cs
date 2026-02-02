using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Contexts.ResourceTypes;

/// <summary>
/// A lazy collection builder for AI context resource types.
/// </summary>
/// <remarks>
/// Resource types are auto-discovered via <see cref="IDiscoverable"/> and the <see cref="AIContextResourceTypeAttribute"/>.
/// Use <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add resource types manually,
/// or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to exclude auto-discovered types.
/// </remarks>
public class AIContextResourceTypeCollectionBuilder
    : LazyCollectionBuilderBase<AIContextResourceTypeCollectionBuilder, AIContextResourceTypeCollection, IAIContextResourceType>
{
    /// <inheritdoc />
    protected override AIContextResourceTypeCollectionBuilder This => this;
}
