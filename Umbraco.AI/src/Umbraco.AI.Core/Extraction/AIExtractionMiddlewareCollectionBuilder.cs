using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// An ordered collection builder for AI extraction middleware.
/// </summary>
/// <remarks>
/// Use this builder to configure the order of middleware in the extraction pipeline:
/// <code>
/// builder.AIExtractionMiddleware()
///     .Append&lt;LoggingExtractionMiddleware&gt;()
///     .InsertBefore&lt;LoggingExtractionMiddleware, TracingMiddleware&gt;();  // Tracing runs before Logging
/// </code>
/// Middleware is applied in collection order when wrapping the underlying extraction client.
/// </remarks>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public class AIExtractionMiddlewareCollectionBuilder
    : OrderedCollectionBuilderBase<AIExtractionMiddlewareCollectionBuilder, AIExtractionMiddlewareCollection, IAIExtractionMiddleware>
{
    /// <inheritdoc />
    protected override AIExtractionMiddlewareCollectionBuilder This => this;
}
