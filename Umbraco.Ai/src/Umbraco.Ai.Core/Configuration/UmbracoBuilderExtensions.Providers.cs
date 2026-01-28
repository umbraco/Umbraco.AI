using Umbraco.Ai.Core.Providers;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for AI provider collection configuration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Gets the AI provider collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI provider collection builder.</returns>
    /// <remarks>
    /// Use this to add or exclude providers from the collection. Example:
    /// <code>
    /// builder.AiProviders()
    ///     .Add&lt;MyCustomProvider&gt;()
    ///     .Exclude&lt;SomeUnwantedProvider&gt;();
    /// </code>
    /// </remarks>
    public static AiProviderCollectionBuilder AiProviders(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AiProviderCollectionBuilder>();
}
