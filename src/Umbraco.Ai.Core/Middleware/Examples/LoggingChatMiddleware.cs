using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Umbraco.Ai.Core.Middleware.Examples;

/// <summary>
/// Example middleware that adds logging to chat operations.
/// This demonstrates how to create custom middleware for cross-cutting concerns.
/// </summary>
/// <remarks>
/// To use this middleware, register it in your Umbraco startup:
/// <code>
/// builder.AddAiChatMiddleware&lt;LoggingChatMiddleware&gt;();
/// </code>
/// </remarks>
public class LoggingChatMiddleware(ILoggerFactory loggerFactory) : IAiChatMiddleware
{
    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        // Use MEAI's built-in logging middleware
        return client.AsBuilder()
            .UseLogging(loggerFactory)
            .Build();
    }

    /// <inheritdoc />
    public int Order => 1000; // High value = applied last (outer layer)
}
