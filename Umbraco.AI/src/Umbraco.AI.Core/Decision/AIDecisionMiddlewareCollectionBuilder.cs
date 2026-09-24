using System.Diagnostics.CodeAnalysis;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// An ordered collection builder for AI decision middleware.
/// </summary>
/// <remarks>
/// Use this builder to configure the order of middleware in the decision pipeline:
/// <code>
/// builder.AIDecisionMiddleware()
///     .Append&lt;LoggingDecisionMiddleware&gt;()
///     .InsertBefore&lt;LoggingDecisionMiddleware, TracingMiddleware&gt;();  // Tracing runs before Logging
/// </code>
/// Middleware is applied in collection order when wrapping the underlying decision client.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public class AIDecisionMiddlewareCollectionBuilder
    : OrderedCollectionBuilderBase<AIDecisionMiddlewareCollectionBuilder, AIDecisionMiddlewareCollection, IAIDecisionMiddleware>
{
    /// <inheritdoc />
    protected override AIDecisionMiddlewareCollectionBuilder This => this;
}
