using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// An ordered collection builder for AI speech-to-text middleware.
/// </summary>
/// <remarks>
/// Use this builder to configure the order of middleware in the speech-to-text pipeline:
/// <code>
/// builder.AISpeechToTextMiddleware()
///     .Append&lt;LoggingSpeechToTextMiddleware&gt;()
///     .Append&lt;CachingMiddleware&gt;()
///     .InsertBefore&lt;LoggingSpeechToTextMiddleware, TracingMiddleware&gt;();  // Tracing runs before Logging
/// </code>
/// Middleware is applied in collection order when wrapping the underlying speech-to-text client.
/// </remarks>
public class AISpeechToTextMiddlewareCollectionBuilder
    : OrderedCollectionBuilderBase<AISpeechToTextMiddlewareCollectionBuilder, AISpeechToTextMiddlewareCollection, IAISpeechToTextMiddleware>
{
    /// <inheritdoc />
    protected override AISpeechToTextMiddlewareCollectionBuilder This => this;
}
