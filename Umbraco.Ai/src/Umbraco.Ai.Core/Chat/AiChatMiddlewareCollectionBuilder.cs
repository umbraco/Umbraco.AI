using Umbraco.Ai.Core.Chat.Middleware;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Chat;

/// <summary>
/// An ordered collection builder for AI chat middleware.
/// </summary>
/// <remarks>
/// Use this builder to configure the order of middleware in the chat pipeline:
/// <code>
/// builder.AiChatMiddleware()
///     .Append&lt;LoggingChatMiddleware&gt;()
///     .Append&lt;CachingMiddleware&gt;()
///     .InsertBefore&lt;LoggingChatMiddleware, TracingMiddleware&gt;();  // Tracing runs before Logging
/// </code>
/// Middleware is applied in collection order when wrapping the underlying chat client.
/// </remarks>
public class AiChatMiddlewareCollectionBuilder
    : OrderedCollectionBuilderBase<AiChatMiddlewareCollectionBuilder, AiChatMiddlewareCollection, IAiChatMiddleware>
{
    /// <inheritdoc />
    protected override AiChatMiddlewareCollectionBuilder This => this;
}
