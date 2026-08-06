using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Agent.Core.Chat;

/// <summary>
/// Places an agent run's runtime-context system prompt at the head of the conversation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ScopedAIAgent"/> composes the prompt from the runtime-context contributors and stages it
/// under <see cref="Constants.ContextKeys.PendingSystemMessage"/> rather than injecting it itself. It
/// can't: on a surface whose history lives server-side (Copilot Workspace), the agent only receives the
/// new turn, and the stored history is prepended below that layer. Index 0 there is not index 0 of what
/// the model sees.
/// </para>
/// <para>
/// This client is the first point that sees history and the new turn as one list, so index 0 here is the
/// real head of the conversation. That matters beyond tidiness: provider prompt caching only reuses a
/// request whose leading tokens match the previous one, and a system block that slides one place further
/// along with every turn moves the point where the two requests diverge back to the start each time.
/// </para>
/// <para>
/// Staging it here also keeps the block out of the agent's request messages, so a persisting history
/// provider no longer stores a copy of it per turn.
/// </para>
/// <para>
/// Only agent runs stage anything, so every other caller (the Prompt package composes its own system
/// message from the same parts, gated on its own setting) is untouched.
/// </para>
/// </remarks>
internal sealed class AIAgentSystemMessageChatClient : DelegatingChatClient
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentSystemMessageChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner chat client.</param>
    /// <param name="runtimeContextAccessor">The runtime context accessor.</param>
    public AIAgentSystemMessageChatClient(
        IChatClient innerClient,
        IAIRuntimeContextAccessor runtimeContextAccessor)
        : base(innerClient)
        => _runtimeContextAccessor = runtimeContextAccessor ?? throw new ArgumentNullException(nameof(runtimeContextAccessor));

    /// <inheritdoc />
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetResponseAsync(InjectSystemMessage(chatMessages), options, cancellationToken);

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.GetStreamingResponseAsync(
            InjectSystemMessage(chatMessages), options, cancellationToken))
        {
            yield return update;
        }
    }

    private IEnumerable<ChatMessage> InjectSystemMessage(IEnumerable<ChatMessage> chatMessages)
    {
        var systemPrompt = _runtimeContextAccessor.Context?.GetValue<string>(Constants.ContextKeys.PendingSystemMessage);
        return string.IsNullOrEmpty(systemPrompt) ? chatMessages : Inject(chatMessages.ToList(), systemPrompt);
    }

    /// <summary>
    /// Puts <paramref name="systemPrompt"/> at index 0, folding it into a system message that is already
    /// there rather than adding a second one. Prepending (rather than appending) preserves the composed
    /// order this pipeline has always produced: runtime context first, then anything a later middleware —
    /// notably the context injector — adds to the same block.
    /// </summary>
    internal static IList<ChatMessage> Inject(IList<ChatMessage> messages, string systemPrompt)
    {
        // Idempotent: an agent run reaches this client once, but a resumed or retried run must not stack
        // a second copy of the same block onto a list that already carries it.
        if (messages.Any(m => m.Role == ChatRole.System && (m.Text?.Contains(systemPrompt, StringComparison.Ordinal) ?? false)))
        {
            return messages;
        }

        if (messages.Count > 0 && messages[0].Role == ChatRole.System)
        {
            var existingContent = messages[0].Text ?? string.Empty;
            messages[0] = new ChatMessage(
                ChatRole.System,
                string.IsNullOrEmpty(existingContent) ? systemPrompt : $"{systemPrompt}\n\n{existingContent}");
            return messages;
        }

        messages.Insert(0, new ChatMessage(ChatRole.System, systemPrompt));
        return messages;
    }
}
