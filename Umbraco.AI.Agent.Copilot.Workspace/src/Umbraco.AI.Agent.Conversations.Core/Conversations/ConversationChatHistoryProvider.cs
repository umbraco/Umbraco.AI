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
    /// Loads the conversation's persisted MAF session-state blob (see
    /// <c>AIConversationEntity.SessionStateJson</c>), or null when the conversation has never run.
    /// Used by the host's stream endpoint to restore a run's session via
    /// <c>AIAgent.DeserializeSessionAsync</c> instead of always starting a bare one — session-scoped
    /// decorators (e.g. the tool-approval-response binder) record their own state directly on the
    /// session object rather than in chat history, so a fresh session per HTTP request would otherwise
    /// lose it.
    /// </summary>
    public async Task<JsonElement?> GetSessionStateAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var json = await _repository.GetSessionStateJsonAsync(conversationId, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<JsonElement>(json, AIJsonUtilities.DefaultOptions);
        }
        catch (JsonException)
        {
            // A corrupt/foreign blob shouldn't fail the run — just start the next session bare.
            return null;
        }
    }

    /// <summary>
    /// Persists the conversation's MAF session-state blob after a run, so the next request's fresh
    /// session can be restored via <c>AIAgent.DeserializeSessionAsync</c> (see <see cref="GetSessionStateAsync"/>).
    /// </summary>
    public async Task SaveSessionStateAsync(Guid conversationId, JsonElement state, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(state, AIJsonUtilities.DefaultOptions);
        await _repository.SetSessionStateJsonAsync(conversationId, json, cancellationToken);
    }

    /// <summary>
    /// Recovers the original approval requests for the given tool-call <paramref name="callIds"/> from the
    /// conversation's persisted history — so a human-approval resume after a reload can correlate against
    /// the real call rather than a synthesised empty one (B2). Returns only the callIds found.
    /// </summary>
    /// <remarks>
    /// Returns the full <see cref="ToolApprovalRequestContent"/> — not just its wrapped tool call — because
    /// its <see cref="ToolApprovalRequestContent.RequestId"/> is FICC's own correlation id (typically
    /// <c>"ficc_" + callId</c>, not the callId itself). <c>Microsoft.Agents.AI</c>'s
    /// <c>ApprovalResponseBindingChatClient</c> matches an inbound <see cref="ToolApprovalResponseContent"/>
    /// against its session-recorded pending requests by that id, silently dropping any response built with
    /// the wrong one — so the resume path must build its response via
    /// <see cref="ToolApprovalRequestContent.CreateResponse(bool, string?)"/> off this recovered request,
    /// not by hand-constructing one from just the callId.
    /// </remarks>
    public async Task<IReadOnlyDictionary<string, ToolApprovalRequestContent>> GetApprovalToolCallsAsync(
        Guid conversationId,
        IReadOnlyCollection<string> callIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, ToolApprovalRequestContent>(StringComparer.Ordinal);
        if (conversationId == Guid.Empty || callIds.Count == 0)
        {
            return result;
        }

        var wanted = callIds.ToHashSet(StringComparer.Ordinal);
        var messages = await LoadDeserializedMessagesAsync(conversationId, cancellationToken);

        foreach (var content in messages.SelectMany(m => m.Contents ?? []))
        {
            if (content is ToolApprovalRequestContent request && wanted.Contains(request.ToolCall.CallId))
            {
                result[request.ToolCall.CallId] = request;
            }
        }

        return result;
    }

    /// <summary>
    /// Finds any <see cref="ToolApprovalRequestContent"/> in the conversation's persisted history that
    /// has no matching <see cref="ToolApprovalResponseContent"/> anywhere in that same history (matched
    /// by <see cref="ToolApprovalRequestContent.RequestId"/>) — i.e. an approval interrupt that was left
    /// pending when the browser was closed or reloaded before Approve/Deny was clicked.
    /// </summary>
    /// <remarks>
    /// MAF's base <c>ChatHistoryProvider.InvokingCoreAsync</c> unconditionally concatenates
    /// <see cref="ProvideChatHistoryAsync"/>'s full result ahead of every run, resume or not. If a
    /// dangling request like this reaches <c>FunctionInvokingChatClient</c> on a plain, non-resume turn,
    /// it throws ("...that have no matching ToolApprovalResponseContent") because nothing in that
    /// combined list resolves it — bricking every subsequent turn in the conversation. The caller uses
    /// this to synthesize a deny for each one before starting a non-resume run (see
    /// <c>AGUIStreamingService.StreamCoreAsync</c>'s <c>staleApprovalRequests</c> handling).
    /// </remarks>
    public async Task<IReadOnlyList<ToolApprovalRequestContent>> GetDanglingApprovalRequestsAsync(
        Guid conversationId, CancellationToken cancellationToken = default)
    {
        if (conversationId == Guid.Empty)
        {
            return [];
        }

        var messages = await LoadDeserializedMessagesAsync(conversationId, cancellationToken);

        var requests = new Dictionary<string, ToolApprovalRequestContent>(StringComparer.Ordinal);
        var respondedRequestIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var content in messages.SelectMany(m => m.Contents ?? []))
        {
            switch (content)
            {
                case ToolApprovalRequestContent request:
                    requests[request.RequestId] = request;
                    break;
                case ToolApprovalResponseContent response:
                    respondedRequestIds.Add(response.RequestId);
                    break;
            }
        }

        return requests
            .Where(kvp => !respondedRequestIds.Contains(kvp.Key))
            .Select(kvp => kvp.Value)
            .ToList();
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

        var messages = await LoadDeserializedMessagesAsync(conversationId, cancellationToken);

        var rehydrated = new List<ChatMessage>(messages.Count);
        foreach (var chatMessage in messages)
        {
            rehydrated.Add(await RehydrateAttachmentsAsync(conversationId, chatMessage, cancellationToken));
        }

        return rehydrated;
    }

    /// <summary>
    /// Loads a conversation's persisted messages and deserializes each one, skipping any that can't be
    /// re-materialized (e.g. a custom <see cref="AIContent"/> type that was later uninstalled) so a
    /// single bad message doesn't fail the whole load (interrogation S14). Shared by every read path
    /// below that needs the conversation's raw message content — attachment rehydration, if needed, is
    /// each caller's own concern (only <see cref="ProvideChatHistoryAsync"/> needs it).
    /// </summary>
    private async Task<IReadOnlyList<ChatMessage>> LoadDeserializedMessagesAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var stored = await _repository.GetMessagesAsync(conversationId, cancellationToken);

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
