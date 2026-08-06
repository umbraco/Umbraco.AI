using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Agent.Core.Chat;

/// <summary>
/// Middleware that places an agent run's runtime-context system prompt at the head of the conversation.
/// </summary>
/// <remarks>
/// <para>
/// Registered outermost, ahead of the context injector, so the system block exists before anything else
/// looks for one and so the audit log's prompt snapshot records what the model was really sent.
/// </para>
/// <para>
/// A no-op for every caller that is not an agent run: only <see cref="ScopedAIAgent"/> stages a prompt
/// under <see cref="Constants.ContextKeys.PendingSystemMessage"/>.
/// </para>
/// </remarks>
public sealed class AIAgentSystemMessageChatMiddleware(IAIRuntimeContextAccessor runtimeContextAccessor)
    : IAIChatMiddleware
{
    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
        => new AIAgentSystemMessageChatClient(client, runtimeContextAccessor);
}
