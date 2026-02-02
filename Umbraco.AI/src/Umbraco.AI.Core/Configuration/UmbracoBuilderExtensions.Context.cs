using Umbraco.AI.Core.Contexts.Resolvers;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for AI Context services registration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Gets the AI context resolver collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI context resolver collection builder.</returns>
    /// <remarks>
    /// Use this to add, remove, or reorder context resolvers. Resolvers are executed in order,
    /// and later resolvers can override resources from earlier resolvers when duplicate IDs exist.
    /// <code>
    /// builder.AIContextResolvers()
    ///     .Append&lt;ProfileContextResolver&gt;()
    ///     .InsertAfter&lt;ProfileContextResolver, AgentContextResolver&gt;();
    /// </code>
    /// </remarks>
    public static AIContextResolverCollectionBuilder AIContextResolvers(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIContextResolverCollectionBuilder>();

    /// <summary>
    /// Gets the AI context resource type collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The resource type collection builder.</returns>
    public static AIContextResourceTypeCollectionBuilder AIContextResourceTypes(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIContextResourceTypeCollectionBuilder>();
}
