using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Defines middleware that can be applied to AI extraction clients.
/// Middleware can implement cross-cutting concerns like tracking, telemetry, caching, etc.
/// </summary>
/// <remarks>
/// The order of middleware execution is controlled by the <see cref="AIExtractionMiddlewareCollectionBuilder"/>
/// using <c>Append</c>, <c>InsertBefore</c>, and <c>InsertAfter</c> methods.
/// </remarks>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public interface IAIExtractionMiddleware
{
    /// <summary>
    /// Applies this middleware to the given extraction client.
    /// </summary>
    /// <param name="client">The extraction client to wrap with middleware.</param>
    /// <returns>The wrapped extraction client with middleware applied.</returns>
    IAIExtractionClient Apply(IAIExtractionClient client);
}
