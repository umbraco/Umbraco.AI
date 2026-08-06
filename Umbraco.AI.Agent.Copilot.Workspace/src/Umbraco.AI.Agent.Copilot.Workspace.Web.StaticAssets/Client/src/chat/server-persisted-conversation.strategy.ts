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

    constructor(repository: UaiConversationRepository) {
        this.#repository = repository;
    }

    /** Binds the strategy to a conversation. Resets the persisted boundary (reset on each open). */
    setConversationId(conversationId: string | undefined): void {
        this.#conversationId = conversationId;
        this.#persisted = 0;
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
            return [];
        }
        const { data } = await this.#repository.requestMessages(id);
        const messages = toDisplayMessages(data?.items ?? []);
        this.#persisted = messages.length;
        return messages;
    }

    outbound(allMessages: UaiChatMessage[]): UaiChatMessage[] {
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
