import type { UaiInterruptContext, UaiInterruptHandler } from "../interrupt.types.js";
import type { UaiInterruptInfo } from "../../types/index.js";

/**
 * Default fallback handler that clears agent state.
 * Used when no specific handler matches the interrupt reason.
 */
export class UaiDefaultInterruptHandler implements UaiInterruptHandler {
    readonly reason = "*";

    handle(_interrupt: UaiInterruptInfo, context: UaiInterruptContext): void {
        context.setAgentState(undefined);
    }
}
