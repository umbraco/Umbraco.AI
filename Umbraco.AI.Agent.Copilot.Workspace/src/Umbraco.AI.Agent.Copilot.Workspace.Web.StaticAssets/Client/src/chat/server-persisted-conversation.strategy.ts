import { UaiAgentClient, UaiHttpAgent, type AgentClientCallbacks } from "@umbraco-ai/agent";
import type { UaiAgentItem, UaiChatMessage, UaiConversationStrategy } from "@umbraco-ai/agent-ui";
import { StreamService } from "../api/sdk.gen.js";
import { UaiConversationRepository } from "../conversation/repository/conversation.repository.js";
import { toDisplayMessages } from "../conversation/message-mapper.js";

/**
 * Server-persisted conversation strategy for the Copilot Workspace.
 *
 * The durable conversation store owns history: the client loads it for display and transmits only
 * the **new turn** to `POST /conversations/{id}/stream-agui` (sending the full array would duplicate,
 * since the server re-supplies persisted history to the model — see `AGUIStreamingService`). The
 * not-yet-persisted boundary (`#persisted`) tracks how many of the controller's messages are already
 * durable; `outbound` returns everything after it. HTTP turns are serial, so by the time `outbound`
 * runs the previous turn is guaranteed persisted.
 *
 * Client creation reuses the shared `UaiHttpAgent` (all AG-UI body/tool/context/resume conversion)
 * with an injected `runner` that redirects the one stream call to the conversation endpoint.
 */
export class UaiServerPersistedConversationStrategy implements UaiConversationStrategy {
    #repository: UaiConversationRepository;
    #conversationId?: string;
    #persisted = 0;
    /**
     * True once {@link loadInitial} has resolved for the *currently bound* conversation. `outbound()`
     * consults this as a last-resort guard: the AG-UI turn endpoint (SSE) carries no authoritative
     * persisted-count or message-id data to resync `#persisted` from — `RunFinishedEvent.Result` is
     * unused today and `MessagesSnapshotEvent` only fires conditionally (file-reference rewrites) with
     * the client's own outbound turn, not the server's durable tail — so this arithmetic boundary is
     * still the source of truth in the normal case. This flag only catches the case where something
     * calls `outbound()` before that arithmetic has ever been initialised for the bound id (e.g. a caller
     * that bypasses the composer-level gate in `conversation-chat-view.element.ts`).
     */
    #loaded = false;

    constructor(repository: UaiConversationRepository) {
        this.#repository = repository;
    }

    /** Binds the strategy to a conversation. Resets the persisted boundary (reset on each open). */
    setConversationId(conversationId: string | undefined): void {
        this.#conversationId = conversationId;
        this.#persisted = 0;
        this.#loaded = false;
    }

    createClient(agent: UaiAgentItem, callbacks: AgentClientCallbacks): UaiAgentClient {
        const transport = new UaiHttpAgent({
            agentId: agent.id,
            runner: async (body, signal) => {
                const id = this.#conversationId;
                if (!id) {
                    throw new Error("No conversation is bound to the Copilot Workspace chat.");
                }
                // The two packages' generated AGUIRunRequestModel types are structurally identical
                // but nominally distinct — bridge with a cast (same pattern as configureAiClient).
                const result = await StreamService.streamAgentAGUI({
                    path: { id },
                    body: body as never,
                    signal,
                });
                return { stream: result.stream as AsyncIterable<unknown> };
            },
        });
        return new UaiAgentClient(transport, callbacks);
    }

    async loadInitial(): Promise<UaiChatMessage[]> {
        const id = this.#conversationId;
        if (!id) {
            this.#persisted = 0;
            this.#loaded = true;
            return [];
        }
        const { data } = await this.#repository.requestMessages(id);
        const messages = toDisplayMessages(data?.items ?? []);
        this.#persisted = messages.length;
        this.#loaded = true;
        return messages;
    }

    /**
     * Refuses to send anything for a bound conversation whose history hasn't loaded yet — the boundary
     * below is meaningless before `loadInitial()` has run for it, and sending here would re-upload the
     * conversation's own persisted history as "new", duplicating it server-side. This is a last-resort
     * guard: `conversation-chat-view.element.ts` already keeps the composer disabled until history load
     * resolves (see `UaiCopilotWorkspaceChatContext.historyLoaded$`), so this should not trigger in the
     * normal flow — it exists for any caller that reaches the strategy without going through that gate.
     */
    outbound(allMessages: UaiChatMessage[]): UaiChatMessage[] {
        if (this.#conversationId && !this.#loaded) {
            console.warn(
                "[UaiServerPersistedConversationStrategy] Refusing to send: history for the bound " +
                    "conversation has not finished loading, so the persisted boundary is not yet known.",
            );
            return [];
        }
        return allMessages.slice(this.#persisted);
    }

    onTurnComplete(allMessages: UaiChatMessage[]): void {
        this.#persisted = allMessages.length;
    }

    /**
     * Regenerate: drop the stored answer to the last user message before the re-run, so the new one
     * replaces it instead of being appended after it. The cutoff is derived server-side — the display
     * list can't address stored rows — so this sends no positions, just the intent.
     *
     * Resetting the boundary to what survives is what makes `outbound` transmit nothing for the re-run:
     * the server answers the user message it already holds. A failed call throws, and the controller then
     * cancels the regenerate with the thread untouched rather than losing the old answer for nothing.
     */
    async onTruncate(remaining: UaiChatMessage[]): Promise<void> {
        const id = this.#conversationId;
        if (!id) {
            return;
        }

        const { error } = await this.#repository.truncateAfterLastUserMessage(id);
        if (error) {
            throw error instanceof Error ? error : new Error(String(error));
        }

        this.#persisted = remaining.length;
    }
}
