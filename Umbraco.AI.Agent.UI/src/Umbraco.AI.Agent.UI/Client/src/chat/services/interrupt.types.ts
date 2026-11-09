import type { UaiAgentState, UaiChatMessage, UaiInterruptInfo } from "../types/index.js";

export interface UaiInterruptHandler {
    readonly reason: string; // e.g., "tool_call", "human_approval", "*" for fallback
    handle(interrupt: UaiInterruptInfo, context: UaiInterruptContext): void;
}

export interface UaiInterruptContext {
    resume(response?: unknown): void;
    setAgentState(state?: UaiAgentState): void;
    readonly lastAssistantMessageId?: string;
    readonly messages: readonly UaiChatMessage[];
    /**
     * Aborted when the user cancels the turn this interrupt belongs to. Handlers should stop work for it;
     * {@link resume} does nothing once it's aborted. An approval card shown through UaiHitlContext closes
     * on abort and calls {@link resume} with no response, meaning cancelled.
     */
    readonly signal?: AbortSignal;
}
