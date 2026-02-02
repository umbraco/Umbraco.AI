using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Chat.Middleware;

/// <summary>
/// Chat middleware that tracks AI chat usage.
/// </summary>
public sealed class AITrackingChatMiddleware : IAIChatMiddleware
{
    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
        => new AITrackingChatClient(client);
}
