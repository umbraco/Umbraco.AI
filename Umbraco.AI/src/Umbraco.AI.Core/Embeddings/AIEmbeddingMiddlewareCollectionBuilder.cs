using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Embeddings;

/// <summary>
/// An ordered collection builder for AI embedding middleware.
/// </summary>
/// <remarks>
/// Use this builder to configure the order of middleware in the embedding pipeline:
/// <code>
/// builder.AIEmbeddingMiddleware()
///     .Append&lt;LoggingEmbeddingMiddleware&gt;()
///     .Append&lt;CachingMiddleware&gt;()
///     .InsertBefore&lt;LoggingEmbeddingMiddleware, TracingMiddleware&gt;();  // Tracing runs before Logging
/// </code>
/// Middleware is applied in collection order when wrapping the underlying embedding generator.
/// </remarks>
public class AIEmbeddingMiddlewareCollectionBuilder
    : OrderedCollectionBuilderBase<AIEmbeddingMiddlewareCollectionBuilder, AIEmbeddingMiddlewareCollection, IAIEmbeddingMiddleware>
{
    /// <inheritdoc />
    protected override AIEmbeddingMiddlewareCollectionBuilder This => this;
}
