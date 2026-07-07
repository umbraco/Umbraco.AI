using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Chat.Middleware;

/// <summary>
/// Chat middleware that records usage analytics and audit entries for chat completions, via the
/// shared <see cref="IAIOperationTracker"/>.
/// </summary>
internal sealed class AITrackingChatMiddleware : IAIChatMiddleware
{
    private readonly IAIOperationTracker _tracker;
    private readonly IAIRuntimeContextAccessor _contextAccessor;

    public AITrackingChatMiddleware(IAIOperationTracker tracker, IAIRuntimeContextAccessor contextAccessor)
    {
        _tracker = tracker;
        _contextAccessor = contextAccessor;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client) => new AITrackingChatClient(client, _tracker, _contextAccessor);
}
