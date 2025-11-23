using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.Middleware;

/// <summary>
/// Defines middleware that can be applied to AI chat clients.
/// Middleware can implement cross-cutting concerns like logging, caching, rate limiting, etc.
/// </summary>
public interface IAiChatMiddleware
{
    /// <summary>
    /// Applies this middleware to the given chat client.
    /// </summary>
    /// <param name="client">The chat client to wrap with middleware.</param>
    /// <returns>The wrapped chat client with middleware applied.</returns>
    IChatClient Apply(IChatClient client);

    /// <summary>
    /// Gets the order in which this middleware should be applied.
    /// Lower values are applied first (closer to the provider).
    /// Higher values are applied last (closer to the caller).
    /// </summary>
    int Order { get; }
}
