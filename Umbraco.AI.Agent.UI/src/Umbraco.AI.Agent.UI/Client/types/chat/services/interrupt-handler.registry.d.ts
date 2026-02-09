import type { UaiInterruptContext, UaiInterruptHandler } from "./interrupt.types.js";
import type { UaiInterruptInfo } from "../types/index.js";
/**
 * Registry for interrupt handlers.
 * Matches interrupts to handlers by reason, with fallback support.
 */
export declare class UaiInterruptHandlerRegistry {
    #private;
    registerAll(handlers: UaiInterruptHandler[]): void;
    register(handler: UaiInterruptHandler): void;
    handle(interrupt: UaiInterruptInfo, context: UaiInterruptContext): boolean;
    clear(): void;
}
//# sourceMappingURL=interrupt-handler.registry.d.ts.map