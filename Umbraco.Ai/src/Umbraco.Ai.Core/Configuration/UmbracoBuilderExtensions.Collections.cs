using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Embeddings;
using Umbraco.Ai.Core.RuntimeContext;
using Umbraco.Ai.Core.Tools;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Extensions;

/// <summary>
/// Extension methods for <see cref="IUmbracoBuilder"/> for AI middleware collection configuration.
/// </summary>
public static partial class UmbracoBuilderExtensions
{
    /// <summary>
    /// Gets the AI chat middleware collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI chat middleware collection builder.</returns>
    /// <remarks>
    /// Use this to add, remove, or reorder chat middleware. Example:
    /// <code>
    /// builder.AiChatMiddleware()
    ///     .Append&lt;LoggingChatMiddleware&gt;()
    ///     .Append&lt;CachingMiddleware&gt;()
    ///     .InsertBefore&lt;LoggingChatMiddleware, TracingMiddleware&gt;();  // Tracing runs before Logging
    /// </code>
    /// </remarks>
    public static AiChatMiddlewareCollectionBuilder AiChatMiddleware(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AiChatMiddlewareCollectionBuilder>();

    /// <summary>
    /// Gets the AI embedding middleware collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI embedding middleware collection builder.</returns>
    /// <remarks>
    /// Use this to add, remove, or reorder embedding middleware. Example:
    /// <code>
    /// builder.AiEmbeddingMiddleware()
    ///     .Append&lt;LoggingEmbeddingMiddleware&gt;()
    ///     .Append&lt;CachingMiddleware&gt;()
    ///     .InsertBefore&lt;LoggingEmbeddingMiddleware, TracingMiddleware&gt;();  // Tracing runs before Logging
    /// </code>
    /// </remarks>
    public static AiEmbeddingMiddlewareCollectionBuilder AiEmbeddingMiddleware(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AiEmbeddingMiddlewareCollectionBuilder>();

    /// <summary>
    /// Gets the AI tools collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI tools collection builder.</returns>
    /// <remarks>
    /// Use this to add or exclude AI tools. Tools are auto-discovered via the [AiTool] attribute.
    /// <code>
    /// builder.AiTools()
    ///     .Add&lt;CustomTool&gt;()
    ///     .Exclude&lt;SomeUnwantedTool&gt;();
    /// </code>
    /// </remarks>
    public static AiToolCollectionBuilder AiTools(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AiToolCollectionBuilder>();

    /// <summary>
    /// Gets the AI runtime context contributor collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI runtime context contributor collection builder.</returns>
    /// <remarks>
    /// Use this to add, remove, or reorder runtime context contributors. Contributors are executed
    /// in order for each context item.
    /// <code>
    /// builder.AiRuntimeContextContributors()
    ///     .Append&lt;SerializedEntityContributor&gt;()
    ///     .Append&lt;CustomContributor&gt;();
    /// </code>
    /// </remarks>
    public static AiRuntimeContextContributorCollectionBuilder AiRuntimeContextContributors(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AiRuntimeContextContributorCollectionBuilder>();
}
