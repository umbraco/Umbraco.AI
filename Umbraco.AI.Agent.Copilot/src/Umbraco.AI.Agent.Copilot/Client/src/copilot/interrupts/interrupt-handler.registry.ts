import type { UaiInterruptContext, UaiInterruptHandler } from "./types.js";
import type { UaiInterruptInfo } from "../types.js";

/**
 * Registry for interrupt handlers.
 * Matches interrupts to handlers by reason, with fallback support.
 */
export class UaiInterruptHandlerRegistry {
    #handlers = new Map<string, UaiInterruptHandler>();
    #fallbackHandler?: UaiInterruptHandler;

    registerAll(handlers: UaiInterruptHandler[]): void {
        for (const handler of handlers) {
            this.register(handler);
        }
    }

    register(handler: UaiInterruptHandler): void {
        if (handler.reason === "*") {
            this.#fallbackHandler = handler;
        } else {
            this.#handlers.set(handler.reason, handler);
        }
    }

    handle(interrupt: UaiInterruptInfo, context: UaiInterruptContext): boolean {
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
