using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Agent.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for AI agent surface collection configuration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Gets the AI agent surface collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI agent surface collection builder.</returns>
    /// <remarks>
    /// Use this to add or exclude surfaces from the collection. Example:
    /// <code>
    /// builder.AIAgentSurfaces()
    ///     .Add&lt;MyCopilotSurface&gt;()
    ///     .Exclude&lt;SomeUnwantedSurface&gt;();
    /// </code>
    /// </remarks>
    public static AIAgentSurfaceCollectionBuilder AIAgentSurfaces(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIAgentSurfaceCollectionBuilder>();
}
