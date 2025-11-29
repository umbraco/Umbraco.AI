using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.Chat;

/// <summary>
/// Defines middleware that can be applied to AI chat clients.
/// Middleware can implement cross-cutting concerns like logging, caching, rate limiting, etc.
/// </summary>
/// <remarks>
/// The order of middleware execution is controlled by the <see cref="AiChatMiddlewareCollectionBuilder"/>
/// using <c>Append</c>, <c>InsertBefore</c>, and <c>InsertAfter</c> methods.
/// </remarks>
public interface IAiChatMiddleware
{
    /// <summary>
    /// Applies this middleware to the given chat client.
    /// </summary>
    /// <param name="client">The chat client to wrap with middleware.</param>
    /// <returns>The wrapped chat client with middleware applied.</returns>
    IChatClient Apply(IChatClient client);
}
