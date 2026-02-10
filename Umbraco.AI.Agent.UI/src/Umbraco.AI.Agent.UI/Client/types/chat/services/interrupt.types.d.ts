import type { UaiAgentState, UaiChatMessage, UaiInterruptInfo } from "../types/index.js";
export interface UaiInterruptHandler {
    readonly reason: string;
    handle(interrupt: UaiInterruptInfo, context: UaiInterruptContext): void;
}
export interface UaiInterruptContext {
    resume(response?: unknown): void;
    setAgentState(state?: UaiAgentState): void;
    readonly lastAssistantMessageId?: string;
    readonly messages: readonly UaiChatMessage[];
}
//# sourceMappingURL=interrupt.types.d.ts.map