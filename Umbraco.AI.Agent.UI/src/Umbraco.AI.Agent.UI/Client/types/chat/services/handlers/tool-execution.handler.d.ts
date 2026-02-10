import type { UaiInterruptContext, UaiInterruptHandler } from "../interrupt.types.js";
import type { UaiFrontendToolManager } from "../frontend-tool.manager.js";
import type { UaiFrontendToolExecutor } from "../frontend-tool.executor.js";
import type { UaiInterruptInfo } from "../../types/index.js";
/**
 * Handles tool_execution interrupts by executing frontend tools.
 *
 * When the server interrupts with reason "tool_execution":
 * 1. Finds frontend tool calls in the last assistant message
 * 2. Executes them via UaiFrontendToolExecutor
 * 3. Resumes the run when all tools complete
 */
export declare class UaiToolExecutionHandler implements UaiInterruptHandler {
    #private;
    private frontendToolManager;
    private executor;
    readonly reason = "tool_execution";
    constructor(frontendToolManager: UaiFrontendToolManager, executor: UaiFrontendToolExecutor);
    handle(_interrupt: UaiInterruptInfo, context: UaiInterruptContext): void;
}
//# sourceMappingURL=tool-execution.handler.d.ts.map