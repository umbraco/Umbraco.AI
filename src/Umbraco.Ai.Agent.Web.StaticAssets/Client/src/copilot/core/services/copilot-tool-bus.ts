import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { Subject } from "rxjs";
import type { ToolCallStatus } from "../types.js";

/**
 * Result of a frontend tool execution.
 */
export interface CopilotToolResult {
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
export interface CopilotToolStatusUpdate {
  /** The ID of the tool call */
  toolCallId: string;
  /** The new status */
  status: ToolCallStatus;
}

/**
 * Lightweight event bus for frontend tool execution.
 * Handles:
 * - Status updates (e.g., "executing", "awaiting_approval")
 * - Result publishing
 *
 * Execution coordination is handled by UaiFrontendToolExecutor,
 * not by this bus.
 */
export class UaiCopilotToolBus extends UmbControllerBase {
  #results = new Subject<CopilotToolResult>();
  readonly results$ = this.#results.asObservable();

  #statusUpdates = new Subject<CopilotToolStatusUpdate>();
  readonly statusUpdates$ = this.#statusUpdates.asObservable();

  constructor(host: UmbControllerHost) {
    super(host);
  }

  /**
   * Publish a tool execution result.
   * @param result The tool execution result
   */
  publishResult(result: CopilotToolResult): void {
    this.#results.next(result);
  }

  /**
   * Publish a status update for a tool call.
   * @param toolCallId The ID of the tool call
   * @param status The new status
   */
  publishStatusUpdate(toolCallId: string, status: ToolCallStatus): void {
    this.#statusUpdates.next({ toolCallId, status });
  }
}
