using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Agent.Core.Chat;

/// <summary>
/// Swaps which channel carries the agent's stable instructions and which carries the volatile
/// runtime-context prompt, so the request's cacheable prefix survives turn-to-turn changes.
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
/// A provider's chat-client adapter appends <c>options.Instructions</c> after any system content already
/// in the message list (see <c>AnthropicPromptCachingWireTests</c>), so whatever sits in that field
/// always lands LAST on the wire, and whatever leads the message list always lands FIRST. Putting the
/// stable instructions at the head of the list -- where a later middleware (the context injector) still
/// appends its own contribution right after them, into the same block -- keeps that combined prefix
/// identical turn after turn. Moving the volatile prompt into <c>options.Instructions</c> instead means
/// it lands after that stable prefix, where its changing content can no longer invalidate the cache
/// entry the stable prefix would otherwise earn.
/// </para>
/// <para>
/// This also preserves the turn-to-turn positional stability the previous "runtime context in the
/// message list" approach relied on: <c>options.Instructions</c> is never part of stored/replayed
/// history, so it can't slide to a new index as a conversation grows the way a message-list entry could.
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
    /// Runs the two steps that swap where each piece of system content lives. See the class remarks for
    /// why each one goes where it does.
    /// </summary>
    internal static (IList<ChatMessage> Messages, ChatOptions? Options) Inject(
        IList<ChatMessage> messages,
        ChatOptions? options,
        string? volatileSystemPrompt)
    {
        messages = MoveAgentInstructionsIntoMessageList(messages, options?.Instructions);
        var newOptions = SetVolatileInstructions(options, volatileSystemPrompt);
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
    /// Replaces <c>options.Instructions</c> with <paramref name="volatileSystemPrompt"/> (or clears it if
    /// there is none), so whatever was there before -- the agent's own instructions, already moved into
    /// the message list by <see cref="MoveAgentInstructionsIntoMessageList"/> -- is never left behind here
    /// too, duplicated on the wire. This is what puts the volatile prompt after the stable block instead
    /// of before it: every provider adapter appends <c>Instructions</c> last.
    /// </summary>
    private static ChatOptions? SetVolatileInstructions(ChatOptions? options, string? volatileSystemPrompt)
    {
        if (options is null && string.IsNullOrEmpty(volatileSystemPrompt))
        {
            return options;
        }

        var newOptions = options?.Clone() ?? new ChatOptions();
        newOptions.Instructions = string.IsNullOrEmpty(volatileSystemPrompt) ? null : volatileSystemPrompt;
        return newOptions;
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
