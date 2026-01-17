using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Contexts.ResourceTypes;

/// <summary>
/// A lazy collection builder for AI context resource types.
/// </summary>
/// <remarks>
/// Resource types are auto-discovered via <see cref="IDiscoverable"/> and the <see cref="AiContextResourceTypeAttribute"/>.
/// Use <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add resource types manually,
/// or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to exclude auto-discovered types.
/// </remarks>
public class AiContextResourceTypeCollectionBuilder
    : LazyCollectionBuilderBase<AiContextResourceTypeCollectionBuilder, AiContextResourceTypeCollection, IAiContextResourceType>
{
    /// <inheritdoc />
    protected override AiContextResourceTypeCollectionBuilder This => this;
}
