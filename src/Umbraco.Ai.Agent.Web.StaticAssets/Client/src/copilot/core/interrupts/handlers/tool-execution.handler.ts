import type { InterruptContext, InterruptHandler } from "../types.js";
import type { UaiFrontendToolManager } from "../../services/frontend-tool-manager.js";
import type { UaiFrontendToolExecutor } from "../../services/frontend-tool-executor.js";
import type { InterruptInfo, ToolCallInfo } from "../../types.js";

/**
 * Handles tool_execution interrupts by executing frontend tools.
 *
 * When the server interrupts with reason "tool_execution":
 * 1. Finds frontend tool calls in the last assistant message
 * 2. Executes them via UaiFrontendToolExecutor
 * 3. Resumes the run when all tools complete
 */
export class UaiToolExecutionHandler implements InterruptHandler {
  readonly reason = "tool_execution";

  constructor(
    private toolManager: UaiFrontendToolManager,
    private executor: UaiFrontendToolExecutor
  ) {}

  handle(_interrupt: InterruptInfo, context: InterruptContext): void {
    const assistantId = context.lastAssistantMessageId;
    const assistantMessage = context.messages.find((msg) => msg.id === assistantId);

    const toolCalls =
      assistantMessage?.toolCalls?.filter((tc) =>
        this.toolManager.isFrontendTool(tc.name)
      ) ?? [];

    if (toolCalls.length > 0) {
      context.setAgentState({ status: "executing", currentStep: "Executing tools..." });
      // Fire-and-forget async execution
      this.#executeAndResume(context, toolCalls);
    } else {
      // No frontend tools to execute
      context.setAgentState(undefined);
    }
  }

  /**
   * Execute tools and resume the run when complete.
   * This is fire-and-forget from handle() - errors are caught per-tool.
   */
  async #executeAndResume(
    context: InterruptContext,
    toolCalls: ToolCallInfo[]
  ): Promise<void> {
    await this.executor.execute(toolCalls);
    context.resume();
  }
}
