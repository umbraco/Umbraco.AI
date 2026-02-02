using Umbraco.AI.Agent.Core.Scopes;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for AI agent scope collection configuration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Gets the AI agent scope collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI agent scope collection builder.</returns>
    /// <remarks>
    /// Use this to add or exclude scopes from the collection. Example:
    /// <code>
    /// builder.AIAgentScopes()
    ///     .Add&lt;MyCopilotScope&gt;()
    ///     .Exclude&lt;SomeUnwantedScope&gt;();
    /// </code>
    /// </remarks>
    public static AIAgentScopeCollectionBuilder AIAgentScopes(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIAgentScopeCollectionBuilder>();
}
