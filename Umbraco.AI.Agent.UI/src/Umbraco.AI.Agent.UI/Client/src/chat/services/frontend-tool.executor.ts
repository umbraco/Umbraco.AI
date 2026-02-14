import { Subject } from "rxjs";
import type { UaiToolRendererManager } from "./tool-renderer.manager.js";
import type { UaiFrontendToolManager } from "./frontend-tool.manager.js";
import type { UaiInterruptInfo, UaiToolCallInfo, UaiToolCallStatus } from "../types/index.js";
import type { UaiInterruptContext } from "./interrupt.types.js";
import type UaiHitlContext from "./hitl.context.js";
import type { ManifestUaiAgentToolRenderer } from "../extensions/uai-agent-tool-renderer.extension.js";

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
export class UaiFrontendToolExecutor {
    #toolRendererManager: UaiToolRendererManager;
    #frontendToolManager: UaiFrontendToolManager;
    #hitlContext?: UaiHitlContext;

    /** Observable streams for tool execution events */
    #results = new Subject<UaiFrontendToolResult>();
    readonly results$ = this.#results.asObservable();

    #statusUpdates = new Subject<UaiFrontendToolStatusUpdate>();
    readonly statusUpdates$ = this.#statusUpdates.asObservable();

    constructor(
        toolRendererManager: UaiToolRendererManager,
        frontendToolManager: UaiFrontendToolManager,
        hitlContext?: UaiHitlContext,
    ) {
        this.#toolRendererManager = toolRendererManager;
        this.#frontendToolManager = frontendToolManager;
        this.#hitlContext = hitlContext;
    }

    /**
     * Set the HITL context for approval handling.
     */
    setHitlContext(hitlContext: UaiHitlContext): void {
        this.#hitlContext = hitlContext;
    }

    /**
     * Execute a list of tool calls sequentially.
     */
    async execute(toolCalls: UaiToolCallInfo[]): Promise<void> {
        for (const toolCall of toolCalls) {
            await this.#executeSingle(toolCall);
        }
    }

    async #executeSingle(toolCall: UaiToolCallInfo): Promise<void> {
        try {
            // Get renderer manifest for approval config
            const rendererManifest = this.#toolRendererManager.getManifest(toolCall.name);

            // Get API from frontend tool manager
            const api = await this.#frontendToolManager.getApi(toolCall.name);

            // Parse arguments
            const args = this.#parseArgs(toolCall.arguments);

            // Check for HITL approval requirement (from renderer manifest)
            if (rendererManifest?.meta.approval && this.#hitlContext) {
                this.#statusUpdates.next({ toolCallId: toolCall.id, status: "awaiting_approval" });

                const approvalResponse = await this.#requestApproval(toolCall, rendererManifest, args);

                if (approvalResponse === null) {
                    this.#results.next({
                        toolCallId: toolCall.id,
                        result: { error: "User cancelled the operation" },
                        error: "User cancelled the operation",
                    });
                    return;
                }

                if (approvalResponse !== undefined) {
                    args.__approval = approvalResponse;
                }
            }

            // Publish executing status
            this.#statusUpdates.next({ toolCallId: toolCall.id, status: "executing" });

            // Execute the tool
            const result = await api.execute(args);

            // Publish success result
            this.#results.next({ toolCallId: toolCall.id, result });
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : String(error);
            this.#results.next({
                toolCallId: toolCall.id,
                result: { error: errorMessage },
                error: errorMessage,
            });
        }
    }

    async #requestApproval(
        toolCall: UaiToolCallInfo,
        rendererManifest: ManifestUaiAgentToolRenderer,
        args: Record<string, unknown>,
    ): Promise<unknown> {
        if (!this.#hitlContext) {
            return undefined;
        }

        const approval = rendererManifest.meta.approval;
        const isSimple = approval === true;
        const approvalObj = typeof approval === "object" ? approval : null;

        const interrupt: UaiInterruptInfo = {
            id: `approval-${toolCall.id}`,
            reason: "tool_approval",
            type: "approval",
            title: `Approve ${rendererManifest.meta.label ?? toolCall.name}`,
            message: `The tool "${rendererManifest.meta.label ?? toolCall.name}" requires your approval to proceed.`,
            options: [
                { value: "approve", label: "Approve", variant: "positive" },
                { value: "deny", label: "Deny", variant: "danger" },
            ],
            payload: {
                toolCallId: toolCall.id,
                toolName: toolCall.name,
                args,
                config: isSimple ? {} : (approvalObj?.config ?? {}),
            },
        };

        return new Promise<unknown>((resolve) => {
            const context: UaiInterruptContext = {
                resume: (response?: unknown) => {
                    if (response === undefined) {
                        resolve(null);
                        return;
                    }

                    let typedResponse: unknown = response;
                    if (typeof response === "string") {
                        try {
                            typedResponse = JSON.parse(response);
                        } catch {
                            typedResponse = response;
                        }
                    }

                    if (typeof typedResponse === "object" && typedResponse !== null) {
                        const obj = typedResponse as Record<string, unknown>;
                        if (obj.approved === false || obj.cancelled === true) {
                            resolve(null);
                            return;
                        }
                    }

                    if (typedResponse === "deny" || typedResponse === "no") {
                        resolve(null);
                        return;
                    }

                    resolve(typedResponse);
                },
                setAgentState: () => {
                    // No-op for frontend approval
                },
                messages: [],
            };

            this.#hitlContext!.setInterrupt(interrupt, context);
        });
    }

    #parseArgs(argsJson: string): Record<string, unknown> {
        try {
            return JSON.parse(argsJson) as Record<string, unknown>;
        } catch {
            return { raw: argsJson };
        }
    }
}
