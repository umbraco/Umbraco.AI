using Umbraco.AI.Agent.Core.Agents;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for the agent user group resolver collection.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Gets the AI agent user group resolver collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI agent user group resolver collection builder.</returns>
    /// <remarks>
    /// Use this to add a resolver for an <c>onBehalfOf</c> kind. Example:
    /// <code>
    /// builder.AIAgentUserGroupResolvers().Append&lt;MyResolver&gt;();
    /// </code>
    /// </remarks>
    public static AIAgentUserGroupResolverCollectionBuilder AIAgentUserGroupResolvers(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIAgentUserGroupResolverCollectionBuilder>();
}
