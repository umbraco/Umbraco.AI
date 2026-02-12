using Umbraco.AI.Core.EntityAdapter;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for entity formatter registration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Gets the entity formatter collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The formatter collection builder.</returns>
    public static AIEntityFormatterCollectionBuilder AIEntityFormatters(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIEntityFormatterCollectionBuilder>();
}
