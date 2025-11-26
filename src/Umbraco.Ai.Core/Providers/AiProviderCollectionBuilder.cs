using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Providers;

/// <summary>
/// A lazy collection builder for AI providers.
/// </summary>
/// <remarks>
/// Providers are auto-discovered via <see cref="IDiscoverable"/> and the <see cref="AiProviderAttribute"/>.
/// Use <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Add{T}"/> to add providers manually,
/// or <see cref="LazyCollectionBuilderBase{TBuilder,TCollection,TItem}.Exclude{T}"/> to exclude auto-discovered providers.
/// </remarks>
public class AiProviderCollectionBuilder
    : LazyCollectionBuilderBase<AiProviderCollectionBuilder, AiProviderCollection, IAiProvider>
{
    /// <inheritdoc />
    protected override AiProviderCollectionBuilder This => this;
}
