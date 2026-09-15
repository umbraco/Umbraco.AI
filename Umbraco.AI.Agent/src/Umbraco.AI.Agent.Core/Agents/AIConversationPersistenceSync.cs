using Microsoft.Extensions.AI;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Bundles the resolvers a persisted-conversation consumer supplies to keep the client's own copy of
/// "what's already saved" honest, on both ends of a turn — see umbraco/Umbraco.AI#375. The Agent layer
/// stays product-agnostic: the consumer closes each resolver over its own conversation store.
/// </summary>
/// <param name="DropAlreadyPersistedLeadingMessages">
/// Incoming side: given the client-supplied messages for a plain (non-resume) turn, returns them with
/// any leading run already duplicated in persisted history removed. A dropped connection can leave the
/// client's own "already sent" bookkeeping stale, so it resends content the server already holds — left
/// in place, MAF's bound <c>ChatHistoryProvider</c> concatenates its persisted copy back in ahead of it,
/// so the model sees the same tool call (and result) twice. Only applied when the run is not itself a
/// resume — a resume turn already has its own dedicated duplicate-handling.
/// </param>
/// <param name="ResolveLastPersistedMessageId">
/// Outgoing side: resolves the <see cref="ChatMessage.MessageId"/> of the last message durably
/// persisted for this conversation once the turn has finished (success or interrupt — either way MAF's
/// bound <c>ChatHistoryProvider</c> has already stored whatever this turn contributed by this point).
/// Carried on the AG-UI <c>RUN_FINISHED</c> event's <c>result</c> so the client can correct its own
/// "already sent" boundary from an authoritative source instead of only inferring it from a turn
/// completing cleanly — the boundary a dropped connection leaves stale in the first place. Returns null
/// when there is nothing persisted yet (a brand-new conversation) or no persistence is configured.
/// </param>
public sealed record AIConversationPersistenceSync(
    Func<IReadOnlyList<ChatMessage>, CancellationToken, ValueTask<IReadOnlyList<ChatMessage>>>? DropAlreadyPersistedLeadingMessages,
    Func<CancellationToken, ValueTask<string?>>? ResolveLastPersistedMessageId);
