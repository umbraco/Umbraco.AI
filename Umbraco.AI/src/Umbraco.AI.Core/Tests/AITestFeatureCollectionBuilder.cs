using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// A lazy collection builder for AI test features (harnesses).
/// </summary>
/// <remarks>
/// Test features are auto-discovered via <see cref="IDiscoverable"/> and the <see cref="AITestFeatureAttribute"/>.
/// Use <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add test features manually,
/// or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to exclude auto-discovered features.
/// </remarks>
public class AITestFeatureCollectionBuilder
    : LazyCollectionBuilderBase<AITestFeatureCollectionBuilder, AITestFeatureCollection, IAITestFeature>
{
    /// <inheritdoc />
    protected override AITestFeatureCollectionBuilder This => this;
}
