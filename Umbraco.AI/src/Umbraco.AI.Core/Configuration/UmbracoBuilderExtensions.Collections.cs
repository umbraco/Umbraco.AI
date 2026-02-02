using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Extensions;

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
    /// builder.AIChatMiddleware()
    ///     .Append&lt;LoggingChatMiddleware&gt;()
    ///     .Append&lt;CachingMiddleware&gt;()
    ///     .InsertBefore&lt;LoggingChatMiddleware, TracingMiddleware&gt;();  // Tracing runs before Logging
    /// </code>
    /// </remarks>
    public static AIChatMiddlewareCollectionBuilder AIChatMiddleware(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIChatMiddlewareCollectionBuilder>();

    /// <summary>
    /// Gets the AI embedding middleware collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI embedding middleware collection builder.</returns>
    /// <remarks>
    /// Use this to add, remove, or reorder embedding middleware. Example:
    /// <code>
    /// builder.AIEmbeddingMiddleware()
    ///     .Append&lt;LoggingEmbeddingMiddleware&gt;()
    ///     .Append&lt;CachingMiddleware&gt;()
    ///     .InsertBefore&lt;LoggingEmbeddingMiddleware, TracingMiddleware&gt;();  // Tracing runs before Logging
    /// </code>
    /// </remarks>
    public static AIEmbeddingMiddlewareCollectionBuilder AIEmbeddingMiddleware(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIEmbeddingMiddlewareCollectionBuilder>();

    /// <summary>
    /// Gets the AI tools collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI tools collection builder.</returns>
    /// <remarks>
    /// Use this to add or exclude AI tools. Tools are auto-discovered via the [AITool] attribute.
    /// <code>
    /// builder.AITools()
    ///     .Add&lt;CustomTool&gt;()
    ///     .Exclude&lt;SomeUnwantedTool&gt;();
    /// </code>
    /// </remarks>
    public static AIToolCollectionBuilder AITools(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIToolCollectionBuilder>();

    /// <summary>
    /// Gets the AI runtime context contributor collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI runtime context contributor collection builder.</returns>
    /// <remarks>
    /// Use this to add, remove, or reorder runtime context contributors. Contributors are executed
    /// in order for each context item.
    /// <code>
    /// builder.AIRuntimeContextContributors()
    ///     .Append&lt;SerializedEntityContributor&gt;()
    ///     .Append&lt;CustomContributor&gt;();
    /// </code>
    /// </remarks>
    public static AIRuntimeContextContributorCollectionBuilder AIRuntimeContextContributors(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIRuntimeContextContributorCollectionBuilder>();

    /// <summary>
    /// Gets the AI versionable entity adapter collection builder.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <returns>The AI versionable entity adapter collection builder.</returns>
    /// <remarks>
    /// Use this to register versionable entity adapters. Core adapters (Connection, Profile, Context)
    /// are registered automatically. Add-on packages can register their own adapters:
    /// <code>
    /// builder.AIVersionableEntityAdapters()
    ///     .Add&lt;PromptVersionableEntityAdapter&gt;();
    /// </code>
    /// </remarks>
    public static AIVersionableEntityAdapterCollectionBuilder AIVersionableEntityAdapters(this IUmbracoBuilder builder)
        => builder.WithCollectionBuilder<AIVersionableEntityAdapterCollectionBuilder>();
}
