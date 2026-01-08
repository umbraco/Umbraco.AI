using Umbraco.Ai.Core.Context.ResourceTypes;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for AI Context services registration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Gets the AI context resource type collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The resource type collection builder.</returns>
    public static AiContextResourceTypeCollectionBuilder AiContextResourceTypes(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AiContextResourceTypeCollectionBuilder>();
}
