import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { BehaviorSubject, Subscription, map } from "rxjs";
import { UaiAgentClient } from "../../transport/uai-agent-client.js";
import { UaiFrontendToolManager } from "../services/frontend-tool-manager.js";
import { UaiFrontendToolExecutor } from "../services/frontend-tool-executor.js";
import type {
  UaiAgentState,
  UaiChatMessage,
  UaiInterruptInfo,
  UaiToolCallInfo,
  UaiToolCallStatus,
} from "../types.js";
import { safeParseJson } from "../utils/json.js";
import { UaiCopilotToolBus, type UaiCopilotToolResult, type UaiCopilotToolStatusUpdate } from "../services/copilot-tool-bus.js";
import type { UaiCopilotAgentItem } from "../repositories/copilot.repository.js";
import { UaiInterruptHandlerRegistry } from "../interrupts/interrupt-handler.registry.js";
import { UaiToolExecutionHandler } from "../interrupts/handlers/tool-execution.handler.js";
import { UaiHitlInterruptHandler } from "../interrupts/handlers/hitl-interrupt.handler.js";
import { UaiDefaultInterruptHandler } from "../interrupts/handlers/default-interrupt.handler.js";
import type { UaiInterruptContext } from "../interrupts/types.js";
import type UaiHitlContext from "../hitl.context.js";

/**
 * Encapsulates the AG-UI client lifecycle, manages chat state + streaming,
 * and exposes RxJS streams that UI components observe.
 */
export class UaiCopilotRunController extends UmbControllerBase {
  #toolBus: UaiCopilotToolBus;
  #toolExecutor: UaiFrontendToolExecutor;
  #client?: UaiAgentClient;
  #agent?: UaiCopilotAgentItem;
  #frontendTools: import("../../transport/types.js").AguiTool[] = [];
  #toolManager = new UaiFrontendToolManager();
  #currentToolCalls: UaiToolCallInfo[] = [];
  #subscriptions: Subscription[] = [];
  #handlerRegistry = new UaiInterruptHandlerRegistry();

  /** ID of the assistant message currently being streamed */
  #currentAssistantMessageId: string | null = null;

  #messages = new BehaviorSubject<UaiChatMessage[]>([]);
  readonly messages$ = this.#messages.asObservable();

  #streamingContent = new BehaviorSubject<string>("");
  readonly streamingContent$ = this.#streamingContent.asObservable();

  #agentState = new BehaviorSubject<UaiAgentState | undefined>(undefined);
  readonly agentState$ = this.#agentState.asObservable();
  readonly isRunning$ = this.agentState$.pipe(map((state) => state !== undefined));

  constructor(host: UmbControllerHost, toolBus: UaiCopilotToolBus, hitlContext: UaiHitlContext) {
    super(host);
    this.#toolBus = toolBus;
    this.#frontendTools = this.#toolManager.loadFromRegistry();
    this.#toolExecutor = new UaiFrontendToolExecutor(host, this.#toolManager, toolBus, hitlContext);
    this.#subscriptions.push(
      this.#toolBus.results$.subscribe((result) => this.#handleToolResult(result)),
      this.#toolBus.statusUpdates$.subscribe((update) => this.#handleToolStatusUpdate(update))
    );
    this.#setupHandlerRegistry();
  }

  override destroy(): void {
    super.destroy();
    this.#subscriptions.forEach((sub) => sub.unsubscribe());
  }

  setAgent(agent: UaiCopilotAgentItem): void {
    if (this.#agent?.id === agent.id) return;
    this.#agent = agent;
    this.#createClient();
    this.resetConversation();
  }

  sendUserMessage(content: string): void {
    if (!this.#client || !content.trim()) return;

    const userMessage: UaiChatMessage = {
      id: crypto.randomUUID(),
      role: "user",
      content,
      timestamp: new Date(),
    };

    const nextMessages = [...this.#messages.value, userMessage];
    this.#messages.next(nextMessages);
    this.#agentState.next({ status: "thinking" });
    this.#client.sendMessage(nextMessages, this.#frontendTools);
  }

  resetConversation(): void {
    this.#messages.next([]);
    this.#streamingContent.next("");
    this.#agentState.next(undefined);
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
    this.#currentToolCalls = [];
    this.#currentAssistantMessageId = null;
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

    // Trigger new generation
    this.#agentState.next({ status: "thinking" });
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

          // Also check if messageId differs from current (indicates new message block from backend)
          const isDifferentMessageId =
            messageId && this.#currentAssistantMessageId && messageId !== this.#currentAssistantMessageId;

          if (isAfterTool || isDifferentMessageId) {
            // Create NEW assistant message for text after tool execution
            // or when backend signals a new message block
            const newMessage: UaiChatMessage = {
              id: messageId || crypto.randomUUID(),
              role: "assistant",
              content: "",
              timestamp: new Date(),
            };
            this.#messages.next([...messages, newMessage]);
            this.#currentAssistantMessageId = newMessage.id;
          } else if (!this.#currentAssistantMessageId) {
            // Create first assistant message for this run
            const newMessage: UaiChatMessage = {
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
          const toolCall: UaiToolCallInfo = { ...info, status: "pending" };
          this.#currentToolCalls = [...this.#currentToolCalls, toolCall];

          let messages = [...this.#messages.value];

          if (!this.#currentAssistantMessageId) {
            // Tool call without preceding text - create empty assistant message
            const newMessage: UaiChatMessage = {
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
          const merged = { ...this.#agentState.value, ...delta } as UaiAgentState;
          this.#agentState.next(merged);
        },
        onMessagesSnapshot: (messages) => this.#messages.next(messages),
        onError: (error) => {
          console.error("Copilot run error:", error);
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
    const toolMessage: UaiChatMessage = {
      id: crypto.randomUUID(),
      role: "tool",
      content: result,
      toolCallId: toolCallId,
      timestamp: new Date(),
    };

    this.#messages.next([...updated, toolMessage]);
  }

  #handleRunFinished(event: { outcome: string; interrupt?: UaiInterruptInfo; error?: string }): void {
    // Clear streaming content
    this.#streamingContent.next("");

    // Store current assistant message ID before resetting
    const assistantMessageId = this.#currentAssistantMessageId;

    // Reset for next run
    this.#currentAssistantMessageId = null;
    this.#currentToolCalls = [];

    if (event.outcome === "error") {
      this.#handleError(event.error);
      return;
    }

    if (event.outcome === "interrupt" && event.interrupt) {
      const context = this.#createInterruptContext(assistantMessageId);
      if (this.#handlerRegistry.handle(event.interrupt, context)) {
        return;
      }
    }

    // Success / no handler available
    this.#agentState.next(undefined);
  }

  #createInterruptContext(assistantMessageId: string | null): UaiInterruptContext {
    return {
      resume: (response?: unknown) => this.#resumeRun(response),
      setAgentState: (state?: UaiAgentState) => this.#agentState.next(state),
      lastAssistantMessageId: assistantMessageId ?? this.#currentAssistantMessageId ?? undefined,
      messages: this.#messages.value,
    };
  }

  #handleError(error?:string): void {
    // Add error message
    const errorMessage: UaiChatMessage = {
      id: crypto.randomUUID(),
      role: "assistant",
      content: `Error: ${error ?? "An error occurred"}`,
      timestamp: new Date(),
    };
    this.#messages.next([...this.#messages.value, errorMessage]);
    this.#agentState.next(undefined);
  }

  #resumeRun(response?: unknown): void {
    // Add response as user message and continue
    if (response !== undefined) {
      const userMessage: UaiChatMessage = {
        id: crypto.randomUUID(),
        role: "user",
        content: typeof response === "string" ? response : JSON.stringify(response),
        timestamp: new Date(),
      };
      this.#messages.next([...this.#messages.value, userMessage]);
    }
    this.#agentState.next({ status: "thinking" });
    this.#client?.sendMessage(this.#messages.value, this.#frontendTools);
  }

  /**
   * Handle frontend tool result from the tool bus.
   * Updates the tool call status in messages and adds a tool message.
   * Note: Resume is handled by UaiToolExecutionHandler when all tools complete.
   */
  #handleToolResult(result: UaiCopilotToolResult): void {
    const resultContent = typeof result.result === "string"
      ? result.result
      : JSON.stringify(result.result);
    const newStatus: UaiToolCallStatus = result.error ? "error" : "completed";

    // Update assistant tool call status
    const updated = this.#messages.value.map((msg) => {
      if (msg.role === "assistant" && msg.toolCalls) {
        return {
          ...msg,
          toolCalls: msg.toolCalls.map((tc) =>
            tc.id === result.toolCallId
              ? { ...tc, status: newStatus, result: resultContent }
              : tc
          ),
        };
      }
      return msg;
    });

    // Append tool message for conversation history
    const toolMessage: UaiChatMessage = {
      id: crypto.randomUUID(),
      role: "tool",
      content: resultContent,
      toolCallId: result.toolCallId,
      timestamp: new Date(),
    };
    this.#messages.next([...updated, toolMessage]);
  }

  /**
   * Handle status update from the tool bus.
   * Updates the tool call status in messages (e.g., "executing", "awaiting_approval").
   */
  #handleToolStatusUpdate(update: UaiCopilotToolStatusUpdate): void {
    const updated = this.#messages.value.map((msg) => {
      if (msg.role === "assistant" && msg.toolCalls) {
        return {
          ...msg,
          toolCalls: msg.toolCalls.map((tc) =>
            tc.id === update.toolCallId ? { ...tc, status: update.status } : tc
          ),
        };
      }
      return msg;
    });
    this.#messages.next(updated);
  }

  #setupHandlerRegistry(): void {
    this.#handlerRegistry.clear();
    this.#handlerRegistry.registerAll([
      new UaiToolExecutionHandler(this.#toolManager, this.#toolExecutor),
      new UaiHitlInterruptHandler(this),
      new UaiDefaultInterruptHandler(),
    ]);
  }
}
