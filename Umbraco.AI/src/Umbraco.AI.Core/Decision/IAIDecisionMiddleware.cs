using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Defines middleware that can be applied to AI decision clients.
/// Middleware can implement cross-cutting concerns like tracking, telemetry, caching, etc.
/// </summary>
/// <remarks>
/// The order of middleware execution is controlled by the <see cref="AIDecisionMiddlewareCollectionBuilder"/>
/// using <c>Append</c>, <c>InsertBefore</c>, and <c>InsertAfter</c> methods.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public interface IAIDecisionMiddleware
{
    /// <summary>
    /// Applies this middleware to the given decision client.
    /// </summary>
    /// <param name="client">The decision client to wrap with middleware.</param>
    /// <returns>The wrapped decision client with middleware applied.</returns>
    IAIDecisionClient Apply(IAIDecisionClient client);
}
