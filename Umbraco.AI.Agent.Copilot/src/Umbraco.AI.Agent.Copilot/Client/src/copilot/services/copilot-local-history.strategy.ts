import { UaiClientOwnedConversationStrategy, type UaiChatMessage } from "@umbraco-ai/agent-ui";
import type { UaiCopilotHistoryStore } from "./copilot-history.store.js";

/**
 * Client-owned conversation strategy that also persists the thread locally, per node.
 *
 * The contextual copilot is client-owned (it accumulates the whole history and re-sends it to the
 * stateless agent endpoint), so this extends {@link UaiClientOwnedConversationStrategy} and keeps its
 * `createClient` / `outbound` behavior unchanged. It adds the two persistence hooks:
 *
 * - {@link loadInitial} — load the active node's saved thread when the conversation (re)opens.
 * - {@link onTurnComplete} — save after each completed turn.
 *
 * The "active node" changes as the user navigates, so the current storage key is read lazily via the
 * injected {@link getKey} accessor rather than captured once. When it returns `undefined` (an unsaved
 * new item with no stable key) nothing is loaded or saved — that conversation stays in memory until
 * the item is saved, at which point the copilot context persists it under the new key.
 */
export class UaiCopilotLocalHistoryStrategy extends UaiClientOwnedConversationStrategy {
    #store: UaiCopilotHistoryStore;
    #getKey: () => string | undefined;

    constructor(store: UaiCopilotHistoryStore, getKey: () => string | undefined) {
        super();
        this.#store = store;
        this.#getKey = getKey;
    }

    override async loadInitial(): Promise<UaiChatMessage[]> {
        const key = this.#getKey();
        return key ? (this.#store.load(key) ?? []) : [];
    }

    // Not an override: onTurnComplete is optional on the interface and the client-owned base omits it.
    onTurnComplete(allMessages: UaiChatMessage[]): void {
        const key = this.#getKey();
        if (key && allMessages.length > 0) {
            this.#store.save(key, allMessages);
        }
    }
}
