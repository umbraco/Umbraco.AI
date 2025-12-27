import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { BehaviorSubject, Subscription } from "rxjs";
import { UaiAgentClient } from "../../transport/uai-agent-client.js";
import { FrontendToolManager } from "../services/frontend-tool-manager.js";
import type {
  AgentState,
  ChatMessage,
  InterruptInfo,
  ToolCallInfo,
} from "../types.js";
import { safeParseJson } from "../utils/json.js";
import { CopilotToolBus, type CopilotToolResult } from "../services/copilot-tool-bus.js";
import type { CopilotAgentItem } from "../repositories/copilot.repository.js";

export interface RunStatusSnapshot {
  isRunning: boolean;
  status?: AgentState["status"];
}

/**
 * Encapsulates the AG-UI client lifecycle, manages chat state + streaming,
 * and exposes RxJS streams that UI components observe.
 */
export class CopilotRunController extends UmbControllerBase {
  #toolBus: CopilotToolBus;
  #client?: UaiAgentClient;
  #agent?: CopilotAgentItem;
  #frontendTools: import("../../transport/types.js").AguiTool[] = [];
  #toolManager = new FrontendToolManager();
  #currentToolCalls: ToolCallInfo[] = [];
  #subscriptions: Subscription[] = [];

  #messages = new BehaviorSubject<ChatMessage[]>([]);
  readonly messages$ = this.#messages.asObservable();

  #streamingContent = new BehaviorSubject<string>("");
  readonly streamingContent$ = this.#streamingContent.asObservable();

  #agentState = new BehaviorSubject<AgentState | undefined>(undefined);
  readonly agentState$ = this.#agentState.asObservable();

  #interrupt = new BehaviorSubject<InterruptInfo | undefined>(undefined);
  readonly interrupt$ = this.#interrupt.asObservable();

  #runStatus = new BehaviorSubject<RunStatusSnapshot>({ isRunning: false });
  readonly runStatus$ = this.#runStatus.asObservable();

  constructor(host: UmbControllerHost, toolBus: CopilotToolBus) {
    super(host);
    this.#toolBus = toolBus;
    this.#frontendTools = this.#toolManager.loadFromRegistry();
    this.#subscriptions.push(
      this.#toolBus.results$.subscribe((result) => this.#handleToolResult(result))
    );
  }

  override destroy(): void {
    super.destroy();
    this.#subscriptions.forEach((sub) => sub.unsubscribe());
  }

  setAgent(agent: CopilotAgentItem): void {
    if (this.#agent?.id === agent.id) return;
    this.#agent = agent;
    this.#createClient();
    this.resetConversation();
  }

  sendUserMessage(content: string): void {
    if (!this.#client || !content.trim()) return;

    const userMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: "user",
      content,
      timestamp: new Date(),
    };

    const nextMessages = [...this.#messages.value, userMessage];
    this.#messages.next(nextMessages);
    this.#agentState.next({ status: "thinking" });
    this.#runStatus.next({ isRunning: true, status: "thinking" });
    this.#client.sendMessage(nextMessages, this.#frontendTools);
  }

  respondToInterrupt(response: string): void {
    if (!this.#client) return;

    this.#interrupt.next(undefined);
    this.#agentState.next({ status: "thinking" });
    this.#runStatus.next({ isRunning: true, status: "thinking" });
    this.#client.resumeRun(response, this.#frontendTools);
  }

  resetConversation(): void {
    this.#messages.next([]);
    this.#streamingContent.next("");
    this.#agentState.next(undefined);
    this.#interrupt.next(undefined);
    this.#currentToolCalls = [];
  }

  /**
   * Abort the current run.
   * Cancels any ongoing agent execution and resets status.
   */
  abortRun(): void {
    if (!this.#client) return;

    this.#client.reset();
    this.#streamingContent.next("");
    this.#agentState.next(undefined);
    this.#runStatus.next({ isRunning: false });
    this.#currentToolCalls = [];
    this.#toolBus.clearPending();
  }

  #createClient(): void {
    if (!this.#agent?.id) return;

    this.#client = UaiAgentClient.create(
      { agentId: this.#agent.id },
      {
        onTextDelta: (delta) => {
          this.#streamingContent.next(this.#streamingContent.value + delta);
        },
        onTextEnd: () => {
          // handled when run finishes
        },
        onToolCallStart: (info) => {
          this.#currentToolCalls = [...this.#currentToolCalls, info];
          this.#agentState.next({ status: "executing", currentStep: `Calling ${info.name}...` });
        },
        onToolCallArgsEnd: (id, args) => this.#handleToolCallArgsEnd(id, args),
        onRunFinished: (event) => this.#handleRunFinished(event),
        onStateSnapshot: (state) => this.#agentState.next(state),
        onStateDelta: (delta) => {
          const merged = { ...this.#agentState.value, ...delta } as AgentState;
          this.#agentState.next(merged);
        },
        onMessagesSnapshot: (messages) => this.#messages.next(messages),
        onError: (error) => {
          console.error("Copilot run error:", error);
          this.#runStatus.next({ isRunning: false });
          this.#agentState.next(undefined);
        },
      }
    );
  }

  #finalizeAssistantMessage(content: string): void {
    if (!content && this.#currentToolCalls.length === 0) return;
    
    const assistantMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: "assistant",
      content,
      toolCalls: this.#currentToolCalls.length > 0 ? [...this.#currentToolCalls] : undefined,
      timestamp: new Date(),
    };

    const nextMessages = [...this.#messages.value, assistantMessage];
    this.#messages.next(nextMessages);
    this.#streamingContent.next("");
    this.#currentToolCalls = [];
  }

  #handleToolCallArgsEnd(toolCallId: string, argsJson: string): void {
    const parsedArgs = safeParseJson(argsJson);
    this.#currentToolCalls = this.#currentToolCalls.map((tc) =>
      tc.id === toolCallId ? { ...tc, arguments: argsJson, parsedArgs } : tc
    );
  }

  #handleRunFinished(event: { outcome: string; interrupt?: InterruptInfo; error?: string }): void {
    this.#finalizeAssistantMessage(this.#streamingContent.value);

    if (event.outcome === "interrupt" && event.interrupt) {
      this.#interrupt.next(event.interrupt);
      this.#agentState.next(undefined);
      this.#runStatus.next({ isRunning: false });
      return;
    }

    if (event.outcome === "error") {
      const nextMessages = [
        ...this.#messages.value,
        {
          id: crypto.randomUUID(),
          role: "assistant",
          content: `Error: ${event.error ?? "An error occurred"}`,
          timestamp: new Date(),
        } as ChatMessage,
      ];

      this.#messages.next(nextMessages);
      this.#agentState.next(undefined);
      this.#runStatus.next({ isRunning: false });
      return;
    }

    const lastMessage = this.#messages.value[this.#messages.value.length - 1];
    const frontendToolCalls =
      lastMessage?.toolCalls?.filter((tc) => this.#toolManager.isFrontendTool(tc.name)) ?? [];

    if (frontendToolCalls.length > 0) {
      const ids = frontendToolCalls.map((tc) => tc.id);
      this.#toolBus.setPending(ids);
      this.#runStatus.next({ isRunning: true, status: "executing" });
      this.#agentState.next({ status: "executing", currentStep: "Executing tools..." });
    } else {
      this.#agentState.next(undefined);
      this.#runStatus.next({ isRunning: false });
    }
  }

  #handleToolResult(result: CopilotToolResult): void {
    // Update existing assistant tool call metadata
    const updated = this.#messages.value.map((msg) => {
      if (msg.role === "assistant" && msg.toolCalls) {
        return {
          ...msg,
          toolCalls: msg.toolCalls.map((tc) =>
            tc.id === result.toolCallId
              ? {
                  ...tc,
                  status: result.error ? "error" : "completed",
                  result:
                    typeof result.result === "string"
                      ? result.result
                      : JSON.stringify(result.result),
                }
              : tc
          ),
        } as ChatMessage;
      }
      return msg;
    });

    // Append tool message for conversation history
    const toolMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: "tool",
      content: typeof result.result === "string" ? result.result : JSON.stringify(result.result),
      toolCallId: result.toolCallId,
      timestamp: new Date(),
    };
    this.#messages.next([...updated, toolMessage]);

    if (!this.#toolBus.hasPending()) {
      this.#agentState.next({ status: "thinking" });
      this.#runStatus.next({ isRunning: true, status: "thinking" });
      this.#client?.sendMessage(this.#messages.value, this.#frontendTools);
    }
  }
}
