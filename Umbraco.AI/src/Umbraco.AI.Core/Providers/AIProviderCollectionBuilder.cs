using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// A lazy collection builder for AI providers.
/// </summary>
/// <remarks>
/// Providers are auto-discovered via <see cref="IDiscoverable"/> and the <see cref="AIProviderAttribute"/>.
/// Use <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add providers manually,
/// or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to exclude auto-discovered providers.
/// </remarks>
public class AIProviderCollectionBuilder
    : LazyCollectionBuilderBase<AIProviderCollectionBuilder, AIProviderCollection, IAiProvider>
{
    /// <inheritdoc />
    protected override AIProviderCollectionBuilder This => this;
}
