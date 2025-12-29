import type { AgentState, ChatMessage, InterruptInfo } from "../types.js";

export interface InterruptHandler {
    readonly reason: string;  // e.g., "tool_execution", "human_approval", "*" for fallback
    handle(interrupt: InterruptInfo, context: InterruptContext): void;
}

export interface InterruptContext {
    resume(response?: unknown): void;
    setAgentState(state?: AgentState): void;
    readonly lastAssistantMessageId?: string;
    readonly messages: readonly ChatMessage[];
}