using Umbraco.AI.Core.ModelFacts;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for AI model fact configuration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Gets the AI model fact provider collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI model fact provider collection builder.</returns>
    /// <remarks>
    /// Use this to add model fact providers or change their order. Example:
    /// <code>
    /// builder.AIModelFactProviders()
    ///     .Append&lt;MyModelFactProvider&gt;();
    /// </code>
    /// </remarks>
    public static AIModelFactProviderCollectionBuilder AIModelFactProviders(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIModelFactProviderCollectionBuilder>();
}
