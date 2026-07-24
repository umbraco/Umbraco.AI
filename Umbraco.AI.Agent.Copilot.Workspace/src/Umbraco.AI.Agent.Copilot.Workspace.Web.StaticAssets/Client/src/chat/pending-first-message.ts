import type { UaiInputContent } from "@umbraco-ai/agent-ui";

/** The first user turn of a draft conversation, handed off across the draft → real-view navigation. */
export interface UaiPendingFirstMessage {
    content: string;
    contentParts?: UaiInputContent[];
}

/**
 * Conversations are only persisted once the user sends their first message (avoids a graveyard of empty
 * "Untitled" conversations). Because opening the freshly-created conversation navigates to a new route —
 * which remounts the conversation view with a *fresh* chat context (see `resolvePageComponent`) — the
 * first turn can't stream in the short-lived draft context. Instead the draft context creates the
 * conversation, stashes the turn here keyed by the new id, and navigates; the newly-mounted real view
 * takes the turn back and streams it through the normal path. This map is process-local and drained on
 * take, so it only ever holds an in-flight handoff.
 */
const pending = new Map<string, UaiPendingFirstMessage>();

/** Stashes the first turn for {@link takePendingFirstMessage} to replay once the real view mounts. */
export function stashPendingFirstMessage(conversationId: string, message: UaiPendingFirstMessage): void {
    pending.set(conversationId, message);
}

/** Removes and returns the stashed first turn for a conversation, if one is awaiting replay. */
export function takePendingFirstMessage(conversationId: string): UaiPendingFirstMessage | undefined {
    const message = pending.get(conversationId);
    pending.delete(conversationId);
    return message;
}
