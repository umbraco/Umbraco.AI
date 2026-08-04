import type { UaiInputContent } from "@umbraco-ai/agent-ui";

/** The first user turn of a draft conversation, handed off across the draft → real-view navigation. */
export interface UaiPendingFirstMessage {
    content: string;
    contentParts?: UaiInputContent[];
}

/**
 * Conversations are only persisted once the user sends their first message (avoids a graveyard of empty
 * "Untitled" conversations). Everything else the user configured on the draft — its project, agent,
 * contexts and resources — is written by that create request, so it needs no handoff. The turn itself
 * can't be persisted, and opening the freshly-created conversation navigates to a new route, which
 * remounts the conversation view with a *fresh* chat context (see `resolvePageComponent`). So the draft
 * context stashes the turn here and navigates; the newly-mounted real view takes it back and streams it
 * through the normal path.
 *
 * A single slot, not a map: only one handoff can ever be in flight. That is what keeps an unclaimed turn
 * from lingering — every persisted open takes, so an abandoned one is dropped on the next navigation
 * rather than replaying if that conversation is opened again later in the tab.
 */
let pending: { conversationId: string; message: UaiPendingFirstMessage } | undefined;

/** Stashes the first turn for {@link takePendingFirstMessage} to replay once the real view mounts. */
export function stashPendingFirstMessage(conversationId: string, message: UaiPendingFirstMessage): void {
    pending = { conversationId, message };
}

/**
 * Clears the slot and returns the stashed turn if it was for this conversation. A mismatch clears it too:
 * the create landed but the user went elsewhere, so the turn is stale and must not replay later.
 */
export function takePendingFirstMessage(conversationId: string): UaiPendingFirstMessage | undefined {
    const stashed = pending;
    pending = undefined;
    return stashed?.conversationId === conversationId ? stashed.message : undefined;
}
