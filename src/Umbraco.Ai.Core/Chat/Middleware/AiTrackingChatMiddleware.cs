using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.Chat.Middleware;

/// <summary>
/// Chat middleware that tracks AI chat usage.
/// </summary>
public sealed class AiTrackingChatMiddleware : IAiChatMiddleware
{
    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
        => new AiTrackingChatClient(client);
}
