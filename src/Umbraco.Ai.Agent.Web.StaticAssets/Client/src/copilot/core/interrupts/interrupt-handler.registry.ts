import type { InterruptContext, InterruptHandler } from "./types.js";
import type { InterruptInfo } from "../types.js";

/**
 * Registry for interrupt handlers.
 * Matches interrupts to handlers by reason, with fallback support.
 */
export class InterruptHandlerRegistry {
  #handlers = new Map<string, InterruptHandler>();
  #fallbackHandler?: InterruptHandler;

  registerAll(handlers: InterruptHandler[]): void {
    for (const handler of handlers) {
      this.register(handler);
    }
  }

  register(handler: InterruptHandler): void {
    if (handler.reason === "*") {
      this.#fallbackHandler = handler;
    } else {
      this.#handlers.set(handler.reason, handler);
    }
  }

  handle(interrupt: InterruptInfo, context: InterruptContext): boolean {
    const handler = this.#handlers.get(interrupt.reason ?? "") ?? this.#fallbackHandler;
    if (handler) {
      handler.handle(interrupt, context);
      return true;
    }
    return false;
  }

  clear(): void {
    this.#handlers.clear();
    this.#fallbackHandler = undefined;
  }
}
