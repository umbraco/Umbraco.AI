using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Agent.Core.Chat;

/// <summary>
/// Puts the agent's stable instructions at the head of the message list and the volatile
/// runtime-context prompt at the tail, so the request's cacheable prefix survives turn-to-turn changes.
/// </summary>
/// <remarks>
/// <para>
/// By the time a run reaches this client, <c>options.Instructions</c> holds the agent's own configured
/// prompt (set once, at agent-build time, from <c>ChatClientAgentOptions.Instructions</c>) and is
/// otherwise untouched -- this client is the outermost chat middleware, so nothing else has had a chance
/// to add to it yet. <see cref="ScopedAIAgent"/> separately composes a volatile prompt from the
/// runtime-context contributors (current entity, surface, etc.) and stages it under
/// <see cref="Constants.ContextKeys.PendingSystemMessage"/> rather than injecting it itself -- it can't:
/// on a surface whose history lives server-side (Copilot Workspace), the agent only receives the new
/// turn, and the stored history is prepended below that layer, so index 0 there is not index 0 of what
/// the model sees.
/// </para>
/// <para>
/// A provider's chat-client adapter pulls every system-role <see cref="ChatMessage"/> out of the message
/// list, in list order, ahead of anything in <c>options.Instructions</c> (see
/// <c>AnthropicPromptCachingWireTests</c>) -- position within the list is what determines wire order, not
/// which field carries the content. So both pieces of content are ordinary messages: the stable
/// instructions are folded into the message at index 0 -- where a later middleware (the context injector)
/// still appends its own contribution right after them, into the same block -- and the volatile prompt is
/// appended as a new message at the end. That keeps the combined leading prefix identical turn after
/// turn, with only the trailing block changing.
/// </para>
/// <para>
/// The volatile message is never left in <c>options.Instructions</c> to also be echoed there, and is
/// never folded into an existing message the way the stable content is: it is freshly computed per
/// request from the current runtime context, and this client's changes to the message list are local to
/// the call -- nothing here is fed back into stored conversation history (see
/// <c>ConversationChatHistoryProvider</c>, which persists from what was passed into the agent run, a
/// layer above this one) -- so appending it fresh every time cannot accumulate stale copies.
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
    {
        var (messages, newOptions) = InjectSystemContent(chatMessages, options);
        return base.GetResponseAsync(messages, newOptions, cancellationToken);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (messages, newOptions) = InjectSystemContent(chatMessages, options);
        await foreach (var update in base.GetStreamingResponseAsync(messages, newOptions, cancellationToken))
        {
            yield return update;
        }
    }

    private (IEnumerable<ChatMessage> Messages, ChatOptions? Options) InjectSystemContent(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options)
    {
        var volatilePrompt = _runtimeContextAccessor.Context?.GetValue<string>(Constants.ContextKeys.PendingSystemMessage);
        if (string.IsNullOrEmpty(options?.Instructions) && string.IsNullOrEmpty(volatilePrompt))
        {
            return (chatMessages, options);
        }

        return Inject(chatMessages.ToList(), options, volatilePrompt);
    }

    /// <summary>
    /// Runs the two steps that place each piece of system content. See the class remarks for why each
    /// one goes where it does.
    /// </summary>
    internal static (IList<ChatMessage> Messages, ChatOptions? Options) Inject(
        IList<ChatMessage> messages,
        ChatOptions? options,
        string? volatileSystemPrompt)
    {
        messages = MoveAgentInstructionsIntoMessageList(messages, options?.Instructions);
        var newOptions = ClearAgentInstructions(options);
        messages = AppendVolatileContextMessage(messages, volatileSystemPrompt);
        return (messages, newOptions);
    }

    /// <summary>
    /// Takes the agent's own instructions out of <c>options.Instructions</c> and puts them at the head of
    /// <paramref name="messages"/> instead -- folding into a leading system message that is already there
    /// rather than adding a second one -- so they land first on the wire.
    /// </summary>
    private static IList<ChatMessage> MoveAgentInstructionsIntoMessageList(IList<ChatMessage> messages, string? agentInstructions)
    {
        if (string.IsNullOrEmpty(agentInstructions))
        {
            return messages;
        }

        // Idempotent: an agent run reaches this client once per HTTP turn, but options.Instructions is
        // re-supplied fresh (from the agent's fixed configuration) on every turn/resume, while the message
        // list carries a prior turn's move forward -- so a later turn must not stack a second copy of the
        // same block onto a list that already carries it.
        var alreadyPresent = messages.Any(m =>
            m.Role == ChatRole.System && (m.Text?.Contains(agentInstructions, StringComparison.Ordinal) ?? false));

        return alreadyPresent ? messages : PrependSystemContent(messages, agentInstructions);
    }

    /// <summary>
    /// Clears <c>options.Instructions</c> once its content has been moved into the message list by
    /// <see cref="MoveAgentInstructionsIntoMessageList"/>, so it is never also echoed there, duplicated
    /// on the wire.
    /// </summary>
    private static ChatOptions? ClearAgentInstructions(ChatOptions? options)
    {
        if (string.IsNullOrEmpty(options?.Instructions))
        {
            return options;
        }

        var newOptions = options.Clone();
        newOptions.Instructions = null;
        return newOptions;
    }

    /// <summary>
    /// Appends the volatile runtime-context prompt as a new system-role message at the end of the list,
    /// so it lands after the stable block that leads it (the provider adapter pulls every system-role
    /// message out of the list, in list order, ahead of anything in <c>options.Instructions</c>).
    /// </summary>
    private static IList<ChatMessage> AppendVolatileContextMessage(IList<ChatMessage> messages, string? volatileSystemPrompt)
    {
        if (string.IsNullOrEmpty(volatileSystemPrompt))
        {
            return messages;
        }

        messages.Add(new ChatMessage(ChatRole.System, volatileSystemPrompt));
        return messages;
    }

    private static IList<ChatMessage> PrependSystemContent(IList<ChatMessage> messages, string content)
    {
        if (messages.Count > 0 && messages[0].Role == ChatRole.System)
        {
            var existingContent = messages[0].Text ?? string.Empty;
            messages[0] = new ChatMessage(
                ChatRole.System,
                string.IsNullOrEmpty(existingContent) ? content : $"{content}\n\n{existingContent}");
            return messages;
        }

        messages.Insert(0, new ChatMessage(ChatRole.System, content));
        return messages;
    }
}
