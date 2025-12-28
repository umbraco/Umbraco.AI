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

  /** ID of the assistant message currently being streamed */
  #currentAssistantMessageId: string | null = null;

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
    this.#currentAssistantMessageId = null;
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
    this.#currentAssistantMessageId = null;
    this.#toolBus.clearPending();
  }

  /**
   * Regenerate the last assistant message.
   * Removes the last assistant message and any subsequent messages,
   * then re-sends the conversation to get a new response.
   */
  regenerateLastMessage(): void {
    if (!this.#client) return;

    const messages = this.#messages.value;

    // Find index of last assistant message
    let lastAssistantIndex = -1;
    for (let i = messages.length - 1; i >= 0; i--) {
      if (messages[i].role === "assistant") {
        lastAssistantIndex = i;
        break;
      }
    }

    if (lastAssistantIndex === -1) return;

    // Remove from that message onwards (assistant message and any tool results after it)
    const truncatedMessages = messages.slice(0, lastAssistantIndex);
    this.#messages.next(truncatedMessages);

    // Reset streaming/tool state
    this.#streamingContent.next("");
    this.#currentToolCalls = [];
    this.#currentAssistantMessageId = null;
    this.#toolBus.clearPending();

    // Trigger new generation
    this.#agentState.next({ status: "thinking" });
    this.#runStatus.next({ isRunning: true, status: "thinking" });
    this.#client.sendMessage(truncatedMessages, this.#frontendTools);
  }

  #createClient(): void {
    if (!this.#agent?.id) return;

    this.#client = UaiAgentClient.create(
      { agentId: this.#agent.id },
      {
        onTextStart: (messageId) => {
          const messages = this.#messages.value;
          const lastMessage = messages[messages.length - 1];

          // Check if we need a new message for text-after-tool:
          // - Last message is a tool result, OR
          // - Last message is an assistant with tool calls
          const isAfterTool =
            lastMessage?.role === "tool" ||
            (lastMessage?.role === "assistant" && lastMessage.toolCalls?.length);

          if (isAfterTool) {
            // Create NEW assistant message for text after tool execution
            const newMessage: ChatMessage = {
              id: messageId || crypto.randomUUID(),
              role: "assistant",
              content: "",
              timestamp: new Date(),
            };
            this.#messages.next([...messages, newMessage]);
            this.#currentAssistantMessageId = newMessage.id;
          } else if (!this.#currentAssistantMessageId) {
            // Create first assistant message for this run
            const newMessage: ChatMessage = {
              id: messageId || crypto.randomUUID(),
              role: "assistant",
              content: "",
              timestamp: new Date(),
            };
            this.#messages.next([...messages, newMessage]);
            this.#currentAssistantMessageId = newMessage.id;
          }
        },
        onTextDelta: (delta) => {
          // Update streamingContent for typing indicator
          this.#streamingContent.next(this.#streamingContent.value + delta);

          // Update current assistant message content directly
          if (this.#currentAssistantMessageId) {
            const messages = this.#messages.value.map((msg) =>
              msg.id === this.#currentAssistantMessageId
                ? { ...msg, content: msg.content + delta }
                : msg
            );
            this.#messages.next(messages);
          }
        },
        onTextEnd: () => {
          // Text complete - nothing special needed
        },
        onToolCallStart: (info) => {
          const toolCall: ToolCallInfo = { ...info, status: "pending" };
          this.#currentToolCalls = [...this.#currentToolCalls, toolCall];

          let messages = [...this.#messages.value];

          if (!this.#currentAssistantMessageId) {
            // Tool call without preceding text - create empty assistant message
            const newMessage: ChatMessage = {
              id: crypto.randomUUID(),
              role: "assistant",
              content: "",
              toolCalls: [toolCall],
              timestamp: new Date(),
            };
            messages.push(newMessage);
            this.#currentAssistantMessageId = newMessage.id;
          } else {
            // Add tool call to current assistant message
            messages = messages.map((msg) =>
              msg.id === this.#currentAssistantMessageId
                ? { ...msg, toolCalls: [...(msg.toolCalls || []), toolCall] }
                : msg
            );
          }

          this.#messages.next(messages);
          this.#agentState.next({ status: "executing", currentStep: `Calling ${info.name}...` });
        },
        onToolCallArgsEnd: (id, args) => this.#handleToolCallArgsEnd(id, args),
        onToolCallResult: (id, result) => this.#handleServerToolResult(id, result),
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

  #handleToolCallArgsEnd(toolCallId: string, argsJson: string): void {
    const parsedArgs = safeParseJson(argsJson);

    // Update currentToolCalls
    this.#currentToolCalls = this.#currentToolCalls.map((tc) =>
      tc.id === toolCallId ? { ...tc, arguments: argsJson, parsedArgs } : tc
    );

    // Update message directly
    const messages = this.#messages.value.map((msg) => {
      if (msg.id === this.#currentAssistantMessageId && msg.toolCalls) {
        return {
          ...msg,
          toolCalls: msg.toolCalls.map((tc) =>
            tc.id === toolCallId ? { ...tc, arguments: argsJson, parsedArgs } : tc
          ),
        };
      }
      return msg;
    });
    this.#messages.next(messages);
  }

  /**
   * Handle server-side tool result (TOOL_CALL_RESULT event).
   * Updates the tool call status and immediately adds the tool message.
   */
  #handleServerToolResult(toolCallId: string, result: string): void {
    // Update pending tool calls
    this.#currentToolCalls = this.#currentToolCalls.map((tc) =>
      tc.id === toolCallId ? { ...tc, status: "completed", result } : tc
    );

    // Update the tool call in the message
    const updated = this.#messages.value.map((msg) => {
      if (msg.id === this.#currentAssistantMessageId && msg.toolCalls) {
        return {
          ...msg,
          toolCalls: msg.toolCalls.map((tc) =>
            tc.id === toolCallId ? { ...tc, status: "completed" as const, result } : tc
          ),
        };
      }
      return msg;
    });

    // Immediately add tool message (must happen before any text-after-tool)
    const toolMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: "tool",
      content: result,
      toolCallId: toolCallId,
      timestamp: new Date(),
    };

    this.#messages.next([...updated, toolMessage]);
  }

  #handleRunFinished(event: { outcome: string; interrupt?: InterruptInfo; error?: string }): void {
    // Clear streaming content
    this.#streamingContent.next("");

    // Store current assistant message ID before resetting
    const assistantMessageId = this.#currentAssistantMessageId;

    // Reset for next run
    this.#currentAssistantMessageId = null;
    this.#currentToolCalls = [];

    if (event.outcome === "interrupt" && event.interrupt) {
      this.#interrupt.next(event.interrupt);
      this.#agentState.next(undefined);
      this.#runStatus.next({ isRunning: false });
      return;
    }

    if (event.outcome === "error") {
      // Add error message
      const errorMessage: ChatMessage = {
        id: crypto.randomUUID(),
        role: "assistant",
        content: `Error: ${event.error ?? "An error occurred"}`,
        timestamp: new Date(),
      };
      this.#messages.next([...this.#messages.value, errorMessage]);
      this.#agentState.next(undefined);
      this.#runStatus.next({ isRunning: false });
      return;
    }

    // Check for frontend tools using the stored assistant message ID
    const assistantMessage = this.#messages.value.find((m) => m.id === assistantMessageId);
    const frontendToolCalls =
      assistantMessage?.toolCalls?.filter((tc) => this.#toolManager.isFrontendTool(tc.name)) ?? [];

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
    const resultContent = typeof result.result === "string"
      ? result.result
      : JSON.stringify(result.result);
    const newStatus = result.error ? "error" : "completed";

    // Update assistant tool call status
    const updated = this.#messages.value.map((msg) => {
      if (msg.role === "assistant" && msg.toolCalls) {
        return {
          ...msg,
          toolCalls: msg.toolCalls.map((tc) =>
            tc.id === result.toolCallId
              ? { ...tc, status: newStatus as ToolCallInfo["status"], result: resultContent }
              : tc
          ),
        };
      }
      return msg;
    });

    // Append tool message for conversation history
    const toolMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: "tool",
      content: resultContent,
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
