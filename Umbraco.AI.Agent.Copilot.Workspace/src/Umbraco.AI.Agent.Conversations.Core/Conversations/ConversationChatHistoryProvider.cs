using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// A MAF <see cref="ChatHistoryProvider"/> that persists conversation history in our EF-backed store
/// (via <see cref="IAIConversationRepository"/>) instead of the LLM service. This is the "third-party /
/// custom storage" pattern: <see cref="ProvideChatHistoryAsync"/> loads prior messages before each run
/// and <see cref="StoreChatHistoryAsync"/> persists the new request + response messages after it.
/// </summary>
/// <remarks>
/// A single instance is shared across all sessions, so it holds NO session-specific state in fields:
/// the bound conversation id lives in the <see cref="AgentSession"/> via <see cref="ProviderSessionState{TState}"/>,
/// and the repository is a DI singleton that opens its own scoped unit-of-work per call.
/// </remarks>
public sealed class ConversationChatHistoryProvider : ChatHistoryProvider
{
    private readonly IAIConversationRepository _repository;
    private readonly ProviderSessionState<ConversationSessionState> _sessionState;

    internal ConversationChatHistoryProvider(IAIConversationRepository repository)
    {
        _repository = repository;
        _sessionState = new ProviderSessionState<ConversationSessionState>(
            stateInitializer: _ => new ConversationSessionState(),
            stateKey: typeof(ConversationChatHistoryProvider).FullName!,
            jsonSerializerOptions: AIJsonUtilities.DefaultOptions);
    }

    /// <inheritdoc />
    public override IReadOnlyList<string> StateKeys => [_sessionState.StateKey];

    /// <summary>
    /// Binds a session to a persisted conversation. Called by the Workspace stream endpoint before the
    /// run so <see cref="ProvideChatHistoryAsync"/>/<see cref="StoreChatHistoryAsync"/> know which
    /// conversation to load/persist.
    /// </summary>
    public void BindConversation(AgentSession session, Guid conversationId)
    {
        var state = _sessionState.GetOrInitializeState(session);
        state.ConversationId = conversationId;
        _sessionState.SaveState(session, state);
    }

    /// <inheritdoc />
    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
        InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        var conversationId = _sessionState.GetOrInitializeState(context.Session).ConversationId;
        if (conversationId == Guid.Empty)
        {
            return [];
        }

        var stored = await _repository.GetMessagesAsync(conversationId, cancellationToken);

        // Deserialize each persisted message; skip any that can't be re-materialized (e.g. a custom
        // AIContent type that was later uninstalled) so a single bad message doesn't fail the whole
        // conversation load (interrogation S14).
        var messages = new List<ChatMessage>(stored.Count);
        foreach (var message in stored)
        {
            var chatMessage = TryDeserialize(message.ContentJson);
            if (chatMessage is not null)
            {
                messages.Add(chatMessage);
            }
        }

        return messages;
    }

    /// <inheritdoc />
    protected override async ValueTask StoreChatHistoryAsync(
        InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        var conversationId = _sessionState.GetOrInitializeState(context.Session).ConversationId;
        if (conversationId == Guid.Empty)
        {
            // No conversation bound to this run (e.g. a non-persisted surface) — nothing to store.
            return;
        }

        // The base InvokedCoreAsync has already filtered out messages that came from ProvideChatHistoryAsync,
        // so RequestMessages here are only genuinely new inbound messages.
        var newMessages = context.RequestMessages
            .Concat(context.ResponseMessages ?? [])
            .Select(ToDomain)
            .ToList();

        if (newMessages.Count == 0)
        {
            return;
        }

        await _repository.AddMessagesAsync(conversationId, newMessages, cancellationToken);
    }

    private static AIMessage ToDomain(ChatMessage message) => new()
    {
        Role = message.Role.Value,
        ContentJson = JsonSerializer.Serialize(message, AIJsonUtilities.DefaultOptions),
        ContentText = message.Text,
        SchemaVersion = 1,
    };

    private static ChatMessage? TryDeserialize(string contentJson)
    {
        try
        {
            return JsonSerializer.Deserialize<ChatMessage>(contentJson, AIJsonUtilities.DefaultOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
