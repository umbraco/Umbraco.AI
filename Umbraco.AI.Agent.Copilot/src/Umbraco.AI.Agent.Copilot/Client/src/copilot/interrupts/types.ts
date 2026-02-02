import type { UaiAgentState, UaiChatMessage, UaiInterruptInfo } from "../types.js";

export interface UaiInterruptHandler {
    readonly reason: string;  // e.g., "tool_execution", "human_approval", "*" for fallback
    handle(interrupt: UaiInterruptInfo, context: UaiInterruptContext): void;
}

export interface UaiInterruptContext {
    resume(response?: unknown): void;
    setAgentState(state?: UaiAgentState): void;
    readonly lastAssistantMessageId?: string;
    readonly messages: readonly UaiChatMessage[];
}