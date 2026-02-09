import type { UaiInterruptContext, UaiInterruptHandler } from "../interrupt.types.js";
import type { UaiFrontendToolManager } from "../frontend-tool.manager.js";
import type { UaiFrontendToolExecutor } from "../frontend-tool.executor.js";
import type { UaiInterruptInfo, UaiToolCallInfo } from "../../types/index.js";

/**
 * Handles tool_execution interrupts by executing frontend tools.
 *
 * When the server interrupts with reason "tool_execution":
 * 1. Finds frontend tool calls in the last assistant message
 * 2. Executes them via UaiFrontendToolExecutor
 * 3. Resumes the run when all tools complete
 */
export class UaiToolExecutionHandler implements UaiInterruptHandler {
    readonly reason = "tool_execution";

    constructor(
        private frontendToolManager: UaiFrontendToolManager,
        private executor: UaiFrontendToolExecutor,
    ) {}

    handle(_interrupt: UaiInterruptInfo, context: UaiInterruptContext): void {
        const assistantId = context.lastAssistantMessageId;
        const assistantMessage = context.messages.find((msg) => msg.id === assistantId);

        const toolCalls =
            assistantMessage?.toolCalls?.filter((tc: UaiToolCallInfo) =>
                this.frontendToolManager.isFrontendTool(tc.name),
            ) ?? [];

        if (toolCalls.length > 0) {
            context.setAgentState({ status: "executing", currentStep: "Executing tools..." });
            this.#executeAndResume(context, toolCalls);
        } else {
            context.setAgentState(undefined);
        }
    }

    async #executeAndResume(context: UaiInterruptContext, toolCalls: UaiToolCallInfo[]): Promise<void> {
        await this.executor.execute(toolCalls);
        context.resume();
    }
}
