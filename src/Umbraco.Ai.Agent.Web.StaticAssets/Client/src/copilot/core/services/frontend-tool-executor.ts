import { loadManifestApi } from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { Subject } from "rxjs";
import type { UaiFrontendToolManager } from "./frontend-tool-manager.js";
import type { UaiInterruptInfo, UaiToolCallInfo, UaiToolCallStatus } from "../types.js";
import type { UaiInterruptContext } from "../interrupts/types.js";
import type { UaiAgentToolApi, ManifestUaiAgentTool } from "../../../agent/tools/uai-agent-tool.extension.js";
import type UaiHitlContext from "../hitl.context.js";

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
 * - Loading tool APIs from manifests
 * - Executing tools sequentially
 * - Handling HITL approval via UaiHitlContext
 * - Publishing status updates and results via observables
 */
export class UaiFrontendToolExecutor {
  #host: UmbControllerHost;
  #toolManager: UaiFrontendToolManager;
  #hitlContext?: UaiHitlContext;

  /** Cache of loaded tool APIs */
  #apiCache = new Map<string, UaiAgentToolApi>();

  /** Observable streams for tool execution events */
  #results = new Subject<UaiFrontendToolResult>();
  readonly results$ = this.#results.asObservable();

  #statusUpdates = new Subject<UaiFrontendToolStatusUpdate>();
  readonly statusUpdates$ = this.#statusUpdates.asObservable();

  constructor(
    host: UmbControllerHost,
    toolManager: UaiFrontendToolManager,
    hitlContext?: UaiHitlContext
  ) {
    this.#host = host;
    this.#toolManager = toolManager;
    this.#hitlContext = hitlContext;
  }

  /**
   * Set the HITL context for approval handling.
   * Called after construction if context wasn't available initially.
   */
  setHitlContext(hitlContext: UaiHitlContext): void {
    this.#hitlContext = hitlContext;
  }

  /**
   * Execute a list of tool calls sequentially.
   * Each tool is executed one at a time, with results published via observables.
   * Errors are caught per-tool and published as error results.
   *
   * @param toolCalls The tool calls to execute
   * @returns Promise that resolves when all tools complete
   */
  async execute(toolCalls: UaiToolCallInfo[]): Promise<void> {
    for (const toolCall of toolCalls) {
      await this.#executeSingle(toolCall);
    }
  }

  /**
   * Execute a single tool call.
   * Catches errors and publishes them as results - never throws.
   */
  async #executeSingle(toolCall: UaiToolCallInfo): Promise<void> {
    try {
      // Get the manifest for this tool
      const manifest = this.#toolManager.getManifest(toolCall.name);
      if (!manifest?.api) {
        throw new Error(`No API found for tool: ${toolCall.name}`);
      }

      // Load or get cached API
      let api = this.#apiCache.get(toolCall.name);
      if (!api) {
        const ApiConstructor = await loadManifestApi<UaiAgentToolApi>(manifest.api);
        if (!ApiConstructor) {
          throw new Error(`Failed to load API for tool: ${toolCall.name}`);
        }
        api = new ApiConstructor(this.#host);
        this.#apiCache.set(toolCall.name, api);
      }

      // Parse arguments
      const args = this.#parseArgs(toolCall.arguments);

      // Check for HITL approval requirement
      if (manifest.meta.approval && this.#hitlContext) {
        // Publish awaiting_approval status
        this.#statusUpdates.next({ toolCallId: toolCall.id, status: "awaiting_approval" });

        // Wait for user approval
        const approvalResponse = await this.#requestApproval(toolCall, manifest, args);

        // If cancelled, publish error and return
        if (approvalResponse === null) {
          this.#results.next({
            toolCallId: toolCall.id,
            result: { error: "User cancelled the operation" },
            error: "User cancelled the operation",
          });
          return;
        }

        // Include approval response in args if needed
        if (approvalResponse !== undefined) {
          args.__approvalResponse = approvalResponse;
        }
      }

      // Publish executing status
      this.#statusUpdates.next({ toolCallId: toolCall.id, status: "executing" });

      // Execute the tool
      const result = await api.execute(args);

      // Publish success result
      this.#results.next({
        toolCallId: toolCall.id,
        result,
      });
    } catch (error) {
      // Publish error result - never throw
      const errorMessage = error instanceof Error ? error.message : String(error);
      this.#results.next({
        toolCallId: toolCall.id,
        result: { error: errorMessage },
        error: errorMessage,
      });
    }
  }

  /**
   * Request user approval via UaiHitlContext.
   * Returns the user's response, or null if cancelled.
   */
  async #requestApproval(
    toolCall: UaiToolCallInfo,
    manifest: ManifestUaiAgentTool,
    args: Record<string, unknown>
  ): Promise<unknown> {
    if (!this.#hitlContext) {
      // No HITL context - proceed without approval
      return undefined;
    }

    const approval = manifest.meta.approval;
    const isSimple = approval === true;
    const approvalObj = typeof approval === "object" ? approval : null;

    // Build interrupt info for the approval UI
    const interrupt: UaiInterruptInfo = {
      id: `approval-${toolCall.id}`,
      reason: "tool_approval",
      type: "approval",
      title: `Approve ${manifest.meta.label ?? toolCall.name}`,
      message: `The tool "${manifest.meta.label ?? toolCall.name}" requires your approval to proceed.`,
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

    // Create a Promise that resolves when user responds
    return new Promise<unknown>((resolve) => {
      // Create UaiInterruptContext where resume() resolves our Promise
      const context: UaiInterruptContext = {
        resume: (response?: unknown) => {
          // "deny" or empty response = cancelled
          if (response === "deny" || response === undefined) {
            resolve(null);
          } else {
            resolve(response);
          }
        },
        setAgentState: () => {
          // No-op for frontend approval - state is managed elsewhere
        },
        messages: [],
      };

      // Show the approval UI
      this.#hitlContext!.setInterrupt(interrupt, context);
    });
  }

  /**
   * Parse tool call arguments from JSON string.
   */
  #parseArgs(argsJson: string): Record<string, unknown> {
    try {
      return JSON.parse(argsJson) as Record<string, unknown>;
    } catch {
      return { raw: argsJson };
    }
  }
}
