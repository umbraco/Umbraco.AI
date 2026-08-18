using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Core.FileStore;

namespace Umbraco.AI.Agent.Conversations.Core.Conversations;

/// <summary>
/// A MAF <see cref="ChatHistoryProvider"/> that persists conversation history in our EF-backed store
/// (via <see cref="IAIConversationRepository"/>) instead of the LLM service. This is the "third-party /
/// custom storage" pattern: <see cref="ProvideChatHistoryAsync"/> loads prior messages before each run
/// and <see cref="StoreChatHistoryAsync"/> persists the new request + response messages after it.
/// </summary>
/// <remarks>
/// <para>
/// A single instance is shared across all sessions, so it holds NO session-specific state in fields:
/// the bound conversation id lives in the <see cref="AgentSession"/> via <see cref="ProviderSessionState{TState}"/>,
/// and the repository is a DI singleton that opens its own scoped unit-of-work per call.
/// </para>
/// <para>
/// A message's <see cref="DataContent"/> parts (an uploaded image, say) are never written into
/// <see cref="AIMessage.ContentJson"/> as-is. <see cref="IAIFileStore"/> is the one place file bytes
/// live; storing them a second time here would leave two independently-aging copies of the same file.
/// Instead, on the way in (<see cref="StoreChatHistoryAsync"/>) each <see cref="DataContent"/> is swapped
/// for a small <see cref="UriContent"/> reference carrying the id it's stored under, and on the way out
/// (<see cref="ProvideChatHistoryAsync"/>) that reference is resolved back into real bytes before the
/// model sees it. See <see cref="AIFileContentMarker"/>.
/// </para>
/// </remarks>
public sealed class ConversationChatHistoryProvider : ChatHistoryProvider
{
    /// <summary>Placeholder shown in place of an attachment the file store no longer has.</summary>
    internal const string MissingAttachmentPlaceholder = "[Attachment no longer available]";

    private readonly IAIConversationRepository _repository;
    private readonly IAIFileStore _fileStore;
    private readonly ProviderSessionState<ConversationSessionState> _sessionState;

    internal ConversationChatHistoryProvider(IAIConversationRepository repository, IAIFileStore fileStore)
    {
        _repository = repository;
        _fileStore = fileStore;
        _sessionState = new ProviderSessionState<ConversationSessionState>(
            stateInitializer: _ => new ConversationSessionState(),
            stateKey: typeof(ConversationChatHistoryProvider).FullName!,
            jsonSerializerOptions: AIJsonUtilities.DefaultOptions);
    }

    /// <inheritdoc />
    public override IReadOnlyList<string> StateKeys => [_sessionState.StateKey];

    /// <summary>
    /// Binds a session to a persisted conversation. Called by the host's stream endpoint before the
    /// run so <see cref="ProvideChatHistoryAsync"/>/<see cref="StoreChatHistoryAsync"/> know which
    /// conversation to load/persist.
    /// </summary>
    public void BindConversation(AgentSession session, Guid conversationId)
    {
        var state = _sessionState.GetOrInitializeState(session);
        state.ConversationId = conversationId;
        _sessionState.SaveState(session, state);
    }

    /// <summary>
    /// Recovers the original tool calls (name + arguments) for the given approval <paramref name="callIds"/>
    /// from the conversation's persisted history — so a human-approval resume after a reload can correlate
    /// against the real call rather than a synthesised empty one (B2). Returns only the callIds found.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, FunctionCallContent>> GetApprovalToolCallsAsync(
        Guid conversationId,
        IReadOnlyCollection<string> callIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, FunctionCallContent>(StringComparer.Ordinal);
        if (conversationId == Guid.Empty || callIds.Count == 0)
        {
            return result;
        }

        var wanted = callIds.ToHashSet(StringComparer.Ordinal);
        var stored = await _repository.GetMessagesAsync(conversationId, cancellationToken);

        foreach (var message in stored)
        {
            var chatMessage = TryDeserialize(message.ContentJson);
            if (chatMessage?.Contents is null)
            {
                continue;
            }

            foreach (var content in chatMessage.Contents)
            {
                // The interrupted run persisted the approval request (FICC emits ToolApprovalRequestContent);
                // fall back to a raw FunctionCallContent in case it was stored pre-promotion.
                var call = content switch
                {
                    ToolApprovalRequestContent { ToolCall: FunctionCallContent fcc } => fcc,
                    FunctionCallContent fcc => fcc,
                    _ => null,
                };

                if (call is not null && wanted.Contains(call.CallId))
                {
                    result[call.CallId] = call;
                }
            }
        }

        return result;
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
                messages.Add(await RehydrateAttachmentsAsync(conversationId, chatMessage, cancellationToken));
            }
        }

        return messages;
    }

    /// <summary>
    /// Resolves any file-store reference in <paramref name="message"/>'s content back into real bytes,
    /// so the model sees the same attachment it was given the turn it was uploaded. A reference whose
    /// file is gone (an operational cleanup edge case) is replaced with a placeholder instead of failing
    /// the whole turn — the rest of the message is still meaningful without it.
    /// </summary>
    internal async Task<ChatMessage> RehydrateAttachmentsAsync(Guid conversationId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        List<AIContent>? rehydrated = null;

        for (var i = 0; i < message.Contents.Count; i++)
        {
            if (message.Contents[i] is not UriContent { AdditionalProperties: { } properties } reference ||
                properties.TryGetValue(AIFileContentMarker.FileIdPropertyKey, out var value) is false ||
                !AIFileContentMarker.TryGetFileId(value, out var fileId))
            {
                continue;
            }

            rehydrated ??= new List<AIContent>(message.Contents);

            var storedFile = await _fileStore.ResolveAsync(conversationId.ToString(), fileId, cancellationToken);
            rehydrated[i] = storedFile is not null
                ? new DataContent(storedFile.Data, storedFile.MimeType) { Name = storedFile.Filename }
                : new TextContent(MissingAttachmentPlaceholder);
        }

        if (rehydrated is null)
        {
            return message;
        }

        return new ChatMessage(message.Role, rehydrated)
        {
            AuthorName = message.AuthorName,
            CreatedAt = message.CreatedAt,
            MessageId = message.MessageId,
            AdditionalProperties = message.AdditionalProperties,
            RawRepresentation = message.RawRepresentation,
        };
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

        var newMessages = await ToStoredMessagesAsync(conversationId, context.RequestMessages, context.ResponseMessages, cancellationToken);

        if (newMessages.Count == 0)
        {
            return;
        }

        await _repository.AddMessagesAsync(conversationId, newMessages, cancellationToken);
    }

    /// <summary>
    /// Selects what a finished run contributes to the durable history. The base <c>InvokedCoreAsync</c> has
    /// already filtered out messages that came from <see cref="ProvideChatHistoryAsync"/>, so the request
    /// messages here are only genuinely new inbound ones.
    /// </summary>
    /// <remarks>
    /// System messages are deliberately dropped. The runtime context (current user, section, serialized
    /// entity, editing guidance) is injected fresh into every run by the agent layer, so storing it would
    /// append an identical block per turn and replay every one of them back to the model on the next turn —
    /// token cost growing with the square of the turn count. Worse, a stored block is a snapshot of the
    /// moment it was written, so an old section/entity context would be replayed alongside, and could
    /// contradict, the current one. Nothing is lost by dropping them: every contributor to that block is
    /// re-derived on the next run either way.
    /// </remarks>
    internal async Task<IReadOnlyList<AIMessage>> ToStoredMessagesAsync(
        Guid conversationId,
        IEnumerable<ChatMessage> requestMessages,
        IEnumerable<ChatMessage>? responseMessages,
        CancellationToken cancellationToken = default)
    {
        var toStore = requestMessages
            .Where(m => m.Role != ChatRole.System)
            .Concat(responseMessages ?? []);

        var result = new List<AIMessage>();
        foreach (var message in toStore)
        {
            result.Add(await ToDomainAsync(conversationId, message, cancellationToken));
        }

        return result;
    }

    private async Task<AIMessage> ToDomainAsync(Guid conversationId, ChatMessage message, CancellationToken cancellationToken)
    {
        var storable = await StripAttachmentsAsync(conversationId, message, cancellationToken);
        return new AIMessage
        {
            Role = storable.Role.Value,
            ContentJson = JsonSerializer.Serialize(storable, AIJsonUtilities.DefaultOptions),
            ContentText = storable.Text,
            SchemaVersion = 2,
        };
    }

    /// <summary>
    /// Replaces each <see cref="DataContent"/> part with a small <see cref="UriContent"/> reference
    /// before this message is serialized to <see cref="AIMessage.ContentJson"/>, so the durable record
    /// never carries a second copy of bytes the file store already owns. A part not already tagged with
    /// a file id (built outside the normal upload pipeline, say) is stored now — this keeps "the database
    /// never holds embedded file bytes" true with no exceptions, not just true for the common case.
    /// </summary>
    private async Task<ChatMessage> StripAttachmentsAsync(Guid conversationId, ChatMessage message, CancellationToken cancellationToken)
    {
        List<AIContent>? stripped = null;

        for (var i = 0; i < message.Contents.Count; i++)
        {
            if (message.Contents[i] is not DataContent data)
            {
                continue;
            }

            stripped ??= new List<AIContent>(message.Contents);

            string? fileId = data.AdditionalProperties?.TryGetValue(AIFileContentMarker.FileIdPropertyKey, out var existing) is true
                && AIFileContentMarker.TryGetFileId(existing, out var existingFileId)
                ? existingFileId
                : null;
            fileId ??= await _fileStore.StoreAsync(conversationId.ToString(), data.Data.ToArray(), data.MediaType, data.Name, cancellationToken);

            stripped[i] = new UriContent(new Uri($"urn:umbraco-ai-agent-file:{fileId}"), data.MediaType)
            {
                AdditionalProperties = new AdditionalPropertiesDictionary
                {
                    [AIFileContentMarker.FileIdPropertyKey] = fileId
                }
            };
        }

        if (stripped is null)
        {
            return message;
        }

        return new ChatMessage(message.Role, stripped)
        {
            AuthorName = message.AuthorName,
            CreatedAt = message.CreatedAt,
            MessageId = message.MessageId,
            AdditionalProperties = message.AdditionalProperties,
            RawRepresentation = message.RawRepresentation,
        };
    }

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
