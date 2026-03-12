using Umbraco.AI.Core.EntityAdapter;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for entity adapter registration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Gets the entity adapter collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The adapter collection builder.</returns>
    public static AIEntityAdapterCollectionBuilder AIEntityAdapters(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIEntityAdapterCollectionBuilder>();
}
