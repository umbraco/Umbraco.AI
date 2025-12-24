import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { BehaviorSubject, Subject } from "rxjs";

export interface CopilotToolResult {
  toolCallId: string;
  result: unknown;
  error?: string;
}

/** Lightweight event bus for coordinating frontend tool execution/results. */
export class CopilotToolBus extends UmbControllerBase {
  #pending = new BehaviorSubject<Set<string>>(new Set());
  readonly pending$ = this.#pending.asObservable();

  #results = new Subject<CopilotToolResult>();
  readonly results$ = this.#results.asObservable();

  constructor(host: UmbControllerHost) {
    super(host);
  }

  setPending(toolCallIds: string[]): void {
    this.#pending.next(new Set(toolCallIds));
  }

  clearPending(): void {
    this.#pending.next(new Set());
  }

  publishResult(result: CopilotToolResult): void {
    // Drop events for unknown tool calls
    if (result.toolCallId) {
      const next = new Set(this.#pending.value);
      next.delete(result.toolCallId);
      this.#pending.next(next);
    }

    this.#results.next(result);
  }

  hasPending(): boolean {
    return this.#pending.value.size > 0;
  }
}
