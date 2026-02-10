import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import type { UaiToolRendererManager } from "./tool-renderer.manager.js";
import type { UaiFrontendToolManager } from "./frontend-tool.manager.js";
import type { UaiInterruptHandler } from "./interrupt.types.js";
import type UaiHitlContext from "./hitl.context.js";
import type { UaiAgentState, UaiChatMessage, UaiAgentItem } from "../types/index.js";
/**
 * Configuration for the run controller.
 * Surfaces inject their tool infrastructure and interrupt handlers.
 */
export interface UaiRunControllerConfig {
    /** Tool renderer manager for manifest/element lookup */
    toolRendererManager: UaiToolRendererManager;
    /** Optional frontend tool manager -- surfaces that support frontend tools inject this */
    frontendToolManager?: UaiFrontendToolManager;
    /** Interrupt handlers to register */
    interruptHandlers: UaiInterruptHandler[];
}
/**
 * Shared run controller for managing AG-UI client lifecycle, chat state, and streaming.
 *
 * Refactored from UaiCopilotRunController to accept tool configuration as optional injection.
 * - Copilot creates with frontendToolManager set + UaiToolExecutionHandler
 * - Chat initially creates without frontendToolManager (server-side tools only)
 */
export declare class UaiRunController extends UmbControllerBase {
    #private;
    readonly messages$: import("rxjs").Observable<UaiChatMessage[]>;
    readonly streamingContent$: import("rxjs").Observable<string>;
    readonly agentState$: import("rxjs").Observable<UaiAgentState | undefined>;
    readonly isRunning$: import("rxjs").Observable<boolean>;
    /** Expose tool renderer manager for context provision */
    get toolRendererManager(): UaiToolRendererManager;
    constructor(host: UmbControllerHost, hitlContext: UaiHitlContext, config: UaiRunControllerConfig);
    destroy(): void;
    setAgent(agent: UaiAgentItem): void;
    sendUserMessage(content: string, context?: Array<{
        description: string;
        value: string;
    }>): void;
    resetConversation(): void;
    abortRun(): void;
    regenerateLastMessage(): void;
}
//# sourceMappingURL=run.controller.d.ts.map