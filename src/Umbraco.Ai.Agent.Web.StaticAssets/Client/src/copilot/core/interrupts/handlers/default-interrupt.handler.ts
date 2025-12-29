import type { InterruptContext, InterruptHandler } from "../types.js";
import type { InterruptInfo } from "../../types.js";

/**
 * Default fallback handler that clears agent state.
 * Used when no specific handler matches the interrupt reason.
 */
export class UaiDefaultInterruptHandler implements InterruptHandler {
  readonly reason = "*";

  handle(_interrupt: InterruptInfo, context: InterruptContext): void {
    context.setAgentState(undefined);
  }
}
