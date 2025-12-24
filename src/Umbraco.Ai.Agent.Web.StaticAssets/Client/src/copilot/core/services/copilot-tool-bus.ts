import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { BehaviorSubject, Subject } from "rxjs";

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
 * Lightweight event bus for coordinating frontend tool execution/results.
 * Handles:
 * - Pending tool tracking (tools awaiting execution)
 * - Executing tool deduplication (prevents duplicate execution across component instances)
 * - Result publishing
 */
export class CopilotToolBus extends UmbControllerBase {
  #pending = new BehaviorSubject<Set<string>>(new Set());
  readonly pending$ = this.#pending.asObservable();

  #results = new Subject<CopilotToolResult>();
  readonly results$ = this.#results.asObservable();

  /** Tracks tool calls currently being executed to prevent duplicate execution */
  #executing = new Set<string>();

  constructor(host: UmbControllerHost) {
    super(host);
  }

  /**
   * Set the list of tool call IDs that are pending execution.
   * @param toolCallIds Array of tool call IDs awaiting execution
   */
  setPending(toolCallIds: string[]): void {
    this.#pending.next(new Set(toolCallIds));
  }

  /**
   * Clear all pending tool calls.
   */
  clearPending(): void {
    this.#pending.next(new Set());
  }

  /**
   * Publish a tool execution result.
   * Removes the tool from pending and emits the result to subscribers.
   * @param result The tool execution result
   */
  publishResult(result: CopilotToolResult): void {
    // Drop events for unknown tool calls
    if (result.toolCallId) {
      const next = new Set(this.#pending.value);
      next.delete(result.toolCallId);
      this.#pending.next(next);
    }

    this.#results.next(result);
  }

  /**
   * Check if there are any pending tool calls.
   */
  hasPending(): boolean {
    return this.#pending.value.size > 0;
  }

  /**
   * Mark a tool as executing.
   * Returns false if already executing (to prevent duplicate execution).
   * @param toolCallId The ID of the tool call
   * @returns true if marked successfully, false if already executing
   */
  markExecuting(toolCallId: string): boolean {
    if (this.#executing.has(toolCallId)) return false;
    this.#executing.add(toolCallId);
    return true;
  }

  /**
   * Clear executing state for a tool.
   * @param toolCallId The ID of the tool call
   */
  clearExecuting(toolCallId: string): void {
    this.#executing.delete(toolCallId);
  }

  /**
   * Check if a tool is currently executing.
   * @param toolCallId The ID of the tool call
   */
  isExecuting(toolCallId: string): boolean {
    return this.#executing.has(toolCallId);
  }
}
