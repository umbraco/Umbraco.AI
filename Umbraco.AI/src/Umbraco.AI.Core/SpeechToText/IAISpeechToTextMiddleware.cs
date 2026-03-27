using Microsoft.Extensions.AI;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// Defines middleware that can be applied to AI speech-to-text clients.
/// Middleware can implement cross-cutting concerns like logging, caching, rate limiting, etc.
/// </summary>
/// <remarks>
/// The order of middleware execution is controlled by the <see cref="AISpeechToTextMiddlewareCollectionBuilder"/>
/// using <c>Append</c>, <c>InsertBefore</c>, and <c>InsertAfter</c> methods.
/// </remarks>
public interface IAISpeechToTextMiddleware
{
    /// <summary>
    /// Applies this middleware to the given speech-to-text client.
    /// </summary>
    /// <param name="client">The speech-to-text client to wrap with middleware.</param>
    /// <returns>The wrapped speech-to-text client with middleware applied.</returns>
    ISpeechToTextClient Apply(ISpeechToTextClient client);
}
