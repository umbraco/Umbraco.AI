using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Tools;

/// <summary>
/// A lazy collection builder for AI tools.
/// </summary>
/// <remarks>
/// Tools are auto-discovered via <see cref="IDiscoverable"/> and the <see cref="AIToolAttribute"/>.
/// Use <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add tools manually,
/// or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to exclude auto-discovered tools.
/// </remarks>
public class AIToolCollectionBuilder
    : LazyCollectionBuilderBase<AIToolCollectionBuilder, AIToolCollection, IAiTool>
{
    /// <inheritdoc />
    protected override AIToolCollectionBuilder This => this;
}
