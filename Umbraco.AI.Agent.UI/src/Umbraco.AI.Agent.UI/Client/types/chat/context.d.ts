import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbContextMinimal } from "@umbraco-cms/backoffice/context-api";
import type { Observable } from "rxjs";
import type { UaiChatMessage, UaiAgentState, UaiInterruptInfo, UaiAgentItem } from "./types/index.js";
import type { PendingApproval } from "./services/hitl.context.js";
import type { UaiToolRendererManager } from "./services/tool-renderer.manager.js";
/**
 * Shared chat context interface.
 *
 * Both UaiCopilotContext and the future UaiChatContext implement this interface.
 * Shared chat components consume UAI_CHAT_CONTEXT. Each surface provides its own implementation.
 *
 * Extends UmbContextMinimal so it can be used with UmbContextToken.
 */
export interface UaiChatContextApi extends UmbContextMinimal {
    /** Observable list of chat messages in the current conversation. */
    readonly messages$: Observable<UaiChatMessage[]>;
    /** Observable for streaming text content during assistant response. */
    readonly streamingContent$: Observable<string>;
    /** Observable for the current agent execution state. */
    readonly agentState$: Observable<UaiAgentState | undefined>;
    /** Observable indicating whether an agent run is in progress. */
    readonly isRunning$: Observable<boolean>;
    /** Observable for HITL interrupt state. */
    readonly hitlInterrupt$: Observable<UaiInterruptInfo | undefined>;
    /** Observable for pending approval with target message for inline rendering. */
    readonly pendingApproval$: Observable<PendingApproval | undefined>;
    /** Observable list of available agents. */
    readonly agents: Observable<UaiAgentItem[]>;
    /** Observable for the currently selected agent. */
    readonly selectedAgent: Observable<UaiAgentItem | undefined>;
    /** Observable for the agent resolved in auto mode (contains agent info from agent_selected event). */
    readonly resolvedAgent$: Observable<{
        agentId: string;
        agentName: string;
        agentAlias: string;
    } | undefined>;
    /** Tool renderer manager for manifest/element lookup. */
    readonly toolRendererManager: UaiToolRendererManager;
    /** Send a user message to the agent. */
    sendUserMessage(content: string): Promise<void>;
    /** Abort the current agent run. */
    abortRun(): void;
    /** Regenerate the last assistant message. */
    regenerateLastMessage(): void;
    /** Select an agent by ID. */
    selectAgent(agentId: string | undefined): void;
    /** Respond to a HITL interrupt. */
    respondToHitl(response: string): void;
}
/**
 * Context token for consuming the shared chat context.
 * This is the primary context that shared chat UI components should consume.
 */
export declare const UAI_CHAT_CONTEXT: UmbContextToken<UaiChatContextApi, UaiChatContextApi>;
//# sourceMappingURL=context.d.ts.map