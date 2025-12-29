import { InterruptContext, InterruptHandler } from "../types.ts";
import { FrontendToolManager } from "../../services/frontend-tool-manager.ts";
import { CopilotToolBus } from "../../services/copilot-tool-bus.ts";
import { InterruptInfo } from "../../types.ts";

export class ToolExecutionHandler implements InterruptHandler {
    
    readonly reason = "tool_execution";

    constructor(
        private toolManager: FrontendToolManager,
        private toolBus: CopilotToolBus
    ) {}

    handle(_interrupt: InterruptInfo, context: InterruptContext): void {
        console.log("ToolExecutionHandler handling tool execution interrupt");
        const assistantId = context.lastAssistantMessageId;
        console.log("  assistantId:", assistantId);
        console.log("  messages count:", context.messages.length);
        console.log("  message ids:", context.messages.map(m => `${m.role}:${m.id}`));

        const assistantMessage = context.messages.find(msg => msg.id === assistantId);
        console.log("  assistantMessage found:", !!assistantMessage);
        console.log("  toolCalls on message:", assistantMessage?.toolCalls?.length ?? 0);

        const toolCalls = assistantMessage?.toolCalls?.filter(
            tc => this.toolManager.isFrontendTool(tc.name)
        ) ?? [];
        console.log("  frontend toolCalls:", toolCalls.length, toolCalls.map(tc => tc.name));

        if (toolCalls.length > 0) {
            const ids = toolCalls.map(tc => tc.id);
            console.log("  Setting pending:", ids);
            this.toolBus.setPending(ids);
            context.setAgentState({ status: "executing", currentStep: "Executing tools..." });
        } else {
            console.log("  No frontend tools found - clearing agent state");
            context.setAgentState(undefined);
        }
    }
}