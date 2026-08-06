import { UaiAgentClient, type AgentClientCallbacks } from "@umbraco-ai/agent";
import type { UaiAgentItem, UaiChatMessage } from "../types/index.js";

/**
 * Pluggable conversation strategy for {@link UaiRunController}. It abstracts the two things that
 * differ between a **client-owned** conversation (contextual Copilot — the client accumulates the
 * whole history and re-sends it to a stateless agent endpoint) and a **server-persisted** one
 * (Copilot Workspace — the durable store owns history; the client loads it for display and transmits
 * only the new turn).
 *
 * The default {@link UaiClientOwnedConversationStrategy} reproduces today's behavior exactly, so
 * surfaces that don't pass a strategy are unaffected.
 */
export interface UaiConversationStrategy {
    /** Creates the AG-UI client for the selected agent (choice of transport/endpoint). */
    createClient(agent: UaiAgentItem, callbacks: AgentClientCallbacks): UaiAgentClient;

    /** Messages to seed the display with when a conversation opens (empty for client-owned). */
    loadInitial(): Promise<UaiChatMessage[]>;

    /**
     * Given the full client-side message list, returns exactly what to transmit for the next run.
     * Client-owned returns the whole list; server-persisted returns only the not-yet-persisted tail.
     */
    outbound(allMessages: UaiChatMessage[]): UaiChatMessage[];

    /** Called after a run finishes successfully, so a persisted strategy can advance its boundary. */
    onTurnComplete?(allMessages: UaiChatMessage[]): void;

    /**
     * Called before a regenerate re-runs the last turn, with the messages that survive the cut (up to and
     * including the user message being answered again). A persisted strategy uses this to drop the stale
     * tail from its durable store, so the new answer replaces the old one instead of being appended after
     * it. Awaited before the run starts, and a rejection cancels the regenerate with the thread untouched.
     */
    onTruncate?(remaining: UaiChatMessage[]): Promise<void>;
}

/**
 * Default strategy: the client owns the conversation. History is accumulated in-memory and sent in
 * full to the agent-keyed endpoint. This is byte-identical to the pre-strategy behavior.
 */
export class UaiClientOwnedConversationStrategy implements UaiConversationStrategy {
    createClient(agent: UaiAgentItem, callbacks: AgentClientCallbacks): UaiAgentClient {
        return UaiAgentClient.create({ agentId: agent.id }, callbacks);
    }

    async loadInitial(): Promise<UaiChatMessage[]> {
        return [];
    }

    outbound(allMessages: UaiChatMessage[]): UaiChatMessage[] {
        return allMessages;
    }
}
