import type { UaiInterruptContext, UaiInterruptHandler } from "../interrupt.types.js";
import type { UaiInterruptInfo } from "../../types/index.js";
/**
 * Default fallback handler that clears agent state.
 * Used when no specific handler matches the interrupt reason.
 */
export declare class UaiDefaultInterruptHandler implements UaiInterruptHandler {
    readonly reason = "*";
    handle(_interrupt: UaiInterruptInfo, context: UaiInterruptContext): void;
}
//# sourceMappingURL=default-interrupt.handler.d.ts.map