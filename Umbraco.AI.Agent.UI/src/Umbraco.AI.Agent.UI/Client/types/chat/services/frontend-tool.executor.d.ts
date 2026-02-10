import type { UaiToolRendererManager } from "./tool-renderer.manager.js";
import type { UaiFrontendToolManager } from "./frontend-tool.manager.js";
import type { UaiToolCallInfo, UaiToolCallStatus } from "../types/index.js";
import type UaiHitlContext from "./hitl.context.js";
/**
 * Result of a frontend tool execution.
 */
export interface UaiFrontendToolResult {
    /** The ID of the tool call this result belongs to */
    toolCallId: string;
    /** The result returned by the tool */
    result: unknown;
    /** Error message if the tool execution failed */
    error?: string;
}
/**
 * Status update for a tool call.
 */
export interface UaiFrontendToolStatusUpdate {
    /** The ID of the tool call */
    toolCallId: string;
    /** The new status */
    status: UaiToolCallStatus;
}
/**
 * Executes frontend tools and publishes results.
 *
 * Responsibilities:
 * - Executing tools sequentially
 * - Handling HITL approval via UaiHitlContext
 * - Publishing status updates and results via observables
 *
 * Surface-agnostic -- works wherever frontend tools are provided.
 */
export declare class UaiFrontendToolExecutor {
    #private;
    readonly results$: import("rxjs").Observable<UaiFrontendToolResult>;
    readonly statusUpdates$: import("rxjs").Observable<UaiFrontendToolStatusUpdate>;
    constructor(toolRendererManager: UaiToolRendererManager, frontendToolManager: UaiFrontendToolManager, hitlContext?: UaiHitlContext);
    /**
     * Set the HITL context for approval handling.
     */
    setHitlContext(hitlContext: UaiHitlContext): void;
    /**
     * Execute a list of tool calls sequentially.
     */
    execute(toolCalls: UaiToolCallInfo[]): Promise<void>;
}
//# sourceMappingURL=frontend-tool.executor.d.ts.map