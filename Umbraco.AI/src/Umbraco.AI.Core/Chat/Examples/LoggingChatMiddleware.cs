using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Umbraco.AI.Core.Chat.Middleware.Examples;

/// <summary>
/// Example middleware that adds logging to chat operations.
/// This demonstrates how to create custom middleware for cross-cutting concerns.
/// </summary>
/// <remarks>
/// To use this middleware, register it in a Composer:
/// <code>
/// public class MyComposer : IComposer
/// {
///     public void Compose(IUmbracoBuilder builder)
///     {
///         builder.AIChatMiddleware()
///             .Append&lt;LoggingChatMiddleware&gt;();
///     }
/// }
/// </code>
/// </remarks>
public class LoggingChatMiddleware(ILoggerFactory loggerFactory) : IAIChatMiddleware
{
    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        // Use MEAI's built-in logging middleware
        return client.AsBuilder()
            .UseLogging(loggerFactory)
            .Build();
    }
}
