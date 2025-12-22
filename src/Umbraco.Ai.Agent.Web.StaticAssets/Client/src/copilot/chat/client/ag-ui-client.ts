import {
  type Message,
  type BaseEvent,
  EventType as AguiEventType,
} from "@ag-ui/client";
import { UaiHttpAgent } from "./uai-http-agent.js";
import { RunStateManager, type StateChangeListener } from "./run-state-manager.js";
import type {
  ChatMessage,
  AgentClientCallbacks,
  ToolCallInfo,
  AgentState,
  InterruptInfo,
  AgUiTool,
  AgentTransport,
  RunLifecycleState,
} from "../types.js";

/**
 * Configuration for the Uai Agent Client.
 */
export interface UaiAgentClientConfig {
  /** Agent ID to connect to */
  agentId: string;
}

/**
 * Client wrapper for AG-UI protocol.
 * Provides a simplified callback interface for the chat UI.
 */
export class UaiAgentClient {
  #transport: AgentTransport;
  #callbacks: AgentClientCallbacks;
  #stateManager: RunStateManager;
  #frontendTools: AgUiTool[] = [];

  /**
   * Create a new UaiAgentClient with an injected transport.
   * For production use, prefer the static create() factory method.
   * @param transport The transport layer for agent communication
   * @param callbacks Optional callbacks for handling events
   */
  constructor(transport: AgentTransport, callbacks: AgentClientCallbacks = {}) {
    this.#transport = transport;
    this.#callbacks = callbacks;
    this.#stateManager = new RunStateManager();
  }

  /**
   * Factory method for creating a UaiAgentClient in production.
   * Creates the appropriate transport layer internally.
   * @param config Configuration for the agent client
   * @param callbacks Optional callbacks for handling events
   * @returns A new UaiAgentClient instance
   */
  static create(config: UaiAgentClientConfig, callbacks?: AgentClientCallbacks): UaiAgentClient {
    const transport = new UaiHttpAgent({ agentId: config.agentId });
    return new UaiAgentClient(transport, callbacks);
  }

  /**
   * Update the callbacks.
   */
  setCallbacks(callbacks: AgentClientCallbacks) {
    this.#callbacks = callbacks;
  }

  /**
   * Get the current messages.
   */
  get messages(): ChatMessage[] {
    return this.#stateManager.messages;
  }

  /**
   * Get the current run lifecycle state.
   */
  get runState(): RunLifecycleState {
    return this.#stateManager.state;
  }

  /**
   * Check if a run is currently active.
   */
  get isRunning(): boolean {
    return this.#stateManager.isRunning;
  }

  /**
   * Subscribe to run state changes.
   * @returns Unsubscribe function
   */
  subscribeToState(listener: StateChangeListener): () => void {
    return this.#stateManager.subscribe(listener);
  }

  /**
   * Register frontend tools that can be called by the agent.
   * These tools are sent to the backend when starting a run.
   */
  setFrontendTools(tools: AgUiTool[]) {
    this.#frontendTools = tools;
  }

  /**
   * Get the registered frontend tools.
   */
  get frontendTools(): AgUiTool[] {
    return [...this.#frontendTools];
  }

  /**
   * Convert ChatMessage to AG-UI Message format.
   */
  #toAgUiMessage(m: ChatMessage): Message {
    if (m.role === "user") {
      return {
        id: m.id,
        role: "user" as const,
        content: m.content,
      };
    } else if (m.role === "assistant") {
      // Include tool calls if present - critical for LLM to know what was already called
      const toolCalls = m.toolCalls?.map(tc => ({
        id: tc.id,
        type: "function" as const,
        function: {
          name: tc.name,
          arguments: tc.arguments ?? "{}",
        },
      }));

      return {
        id: m.id,
        role: "assistant" as const,
        content: m.content,
        ...(toolCalls && toolCalls.length > 0 && { toolCalls }),
      };
    } else {
      // tool message requires toolCallId
      return {
        id: m.id,
        role: "tool" as const,
        content: m.content,
        toolCallId: m.toolCallId ?? m.id,
      };
    }
  }

  /**
   * Send a message and start a new run.
   * @param messages The messages to send
   * @param tools Optional additional tools to include (merged with registered frontend tools)
   */
  async sendMessage(messages: ChatMessage[], tools?: AgUiTool[]): Promise<void> {
    const threadId = crypto.randomUUID();
    const runId = crypto.randomUUID();

    // Initialize state manager for this run
    this.#stateManager.startRun(runId, threadId, messages);

    // Set messages on the agent before running
    const convertedMessages = messages.map((m) => this.#toAgUiMessage(m));
    this.#transport.setMessages(convertedMessages);

    // Merge frontend tools with any additional tools passed in
    const allTools = [...this.#frontendTools, ...(tools ?? [])];

    try {
      // Subscribe to the transport's event stream
      this.#transport.run({
        threadId,
        runId,
        messages: convertedMessages,
        tools: allTools,
        context: [],
      }).subscribe({
        next: (event) => {
          this.#handleEvent(event);
        },
        error: (error) => {
          const err = error instanceof Error ? error : new Error(String(error));
          this.#stateManager.setError(err);
          this.#callbacks.onError?.(err);
        },
        complete: () => {
          // Stream completed
        },
      });
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      this.#stateManager.setError(err);
      this.#callbacks.onError?.(err);
    }
  }

  /**
   * Resume a run after an interrupt with user response.
   */
  async resumeRun(interruptResponse: string): Promise<void> {
    const userMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: "user",
      content: interruptResponse,
      timestamp: new Date(),
    };

    await this.sendMessage([...this.#stateManager.messages, userMessage]);
  }

  /**
   * Handle incoming AG-UI events.
   */
  #handleEvent(event: BaseEvent) {
    switch (event.type) {
      case AguiEventType.TEXT_MESSAGE_START:
        // Message started - nothing to do
        break;

      case AguiEventType.TEXT_MESSAGE_CONTENT:
        this.#callbacks.onTextDelta?.((event as BaseEvent & { delta: string }).delta);
        break;

      case AguiEventType.TEXT_MESSAGE_END:
        this.#callbacks.onTextEnd?.();
        break;

      case AguiEventType.TOOL_CALL_START:
        this.#handleToolCallStart(event as BaseEvent & { toolCallId: string; toolCallName: string });
        break;

      case AguiEventType.TOOL_CALL_ARGS:
        this.#handleToolCallArgs(event as BaseEvent & { toolCallId: string; delta: string });
        break;

      case AguiEventType.TOOL_CALL_END:
        this.#handleToolCallEnd(event as BaseEvent & { toolCallId: string });
        break;

      case AguiEventType.TOOL_CALL_RESULT:
        this.#handleToolCallResult(event as BaseEvent & { toolCallId: string; content: string });
        break;

      case AguiEventType.RUN_FINISHED:
        this.#handleRunFinished(event as BaseEvent & { outcome: string; interrupt?: unknown; error?: string });
        break;

      case AguiEventType.RUN_ERROR:
        this.#callbacks.onError?.(new Error((event as BaseEvent & { message: string }).message));
        break;

      case AguiEventType.STATE_SNAPSHOT:
        this.#callbacks.onStateSnapshot?.((event as BaseEvent & { state: AgentState }).state);
        break;

      case AguiEventType.STATE_DELTA:
        this.#callbacks.onStateDelta?.((event as BaseEvent & { delta: Partial<AgentState> }).delta);
        break;

      case AguiEventType.MESSAGES_SNAPSHOT:
        this.#handleMessagesSnapshot(event as BaseEvent & { messages: unknown[] });
        break;
    }
  }

  #handleToolCallStart(event: BaseEvent & { toolCallId: string; toolCallName: string }) {
    const toolCall: ToolCallInfo = {
      id: event.toolCallId,
      name: event.toolCallName,
      arguments: "",
      status: "pending",
    };

    this.#stateManager.addToolCall(toolCall);
    this.#callbacks.onToolCallStart?.(toolCall);
  }

  #handleToolCallArgs(event: BaseEvent & { toolCallId: string; delta: string }) {
    this.#stateManager.appendToolCallArgs(event.toolCallId, event.delta);
  }

  #handleToolCallEnd(event: BaseEvent & { toolCallId: string }) {
    const toolCallId = event.toolCallId;
    const args = this.#stateManager.finalizeToolCallArgs(toolCallId);

    if (args !== undefined) {
      // Status stays "pending" for backend tools (waiting for TOOL_CALL_RESULT)
      // Frontend tools will set status to "executing" when they execute
      this.#callbacks.onToolCallArgsEnd?.(toolCallId, args);
      this.#callbacks.onToolCallEnd?.(toolCallId);
    }
  }

  #handleToolCallResult(event: BaseEvent & { toolCallId: string; content: string }) {
    const toolCallId = event.toolCallId;
    const content = event.content;

    this.#stateManager.setToolCallResult(toolCallId, content, "completed");
    this.#callbacks.onToolCallResult?.(toolCallId, content);
  }

  #handleRunFinished(event: BaseEvent & { outcome: string; interrupt?: unknown; error?: string }) {
    const outcome = event.outcome;

    if (outcome === "interrupt") {
      const interrupt = this.#parseInterrupt(event.interrupt);
      this.#stateManager.interrupt(interrupt);
      this.#callbacks.onRunFinished?.({
        outcome: "interrupt",
        interrupt,
      });
    } else if (outcome === "error") {
      this.#stateManager.setError(new Error(event.error ?? "Unknown error"));
      this.#callbacks.onRunFinished?.({
        outcome: "error",
        error: event.error as string,
      });
    } else {
      this.#stateManager.complete();
      this.#callbacks.onRunFinished?.({
        outcome: "success",
      });
    }
  }

  #handleMessagesSnapshot(event: BaseEvent & { messages: unknown[] }) {
    const rawMessages = event.messages as Array<{
      id: string;
      role: string;
      content: string;
    }>;

    const messages: ChatMessage[] = rawMessages.map((m) => ({
      id: m.id,
      role: m.role as "user" | "assistant" | "tool",
      content: m.content,
      timestamp: new Date(),
    }));

    this.#stateManager.setMessages(messages);
    this.#callbacks.onMessagesSnapshot?.(messages);
  }

  #parseInterrupt(raw: unknown): InterruptInfo {
    const data = raw as Record<string, unknown>;

    return {
      id: (data.id as string) ?? crypto.randomUUID(),
      type: (data.type as InterruptInfo["type"]) ?? "custom",
      title: (data.title as string) ?? "Action Required",
      message: (data.message as string) ?? "",
      options: data.options as InterruptInfo["options"],
      inputConfig: data.inputConfig as InterruptInfo["inputConfig"],
      metadata: data.metadata as Record<string, unknown>,
    };
  }

  /**
   * Mark a tool call as completed with result.
   */
  setToolCallResult(toolCallId: string, result: unknown) {
    const resultStr = typeof result === "string" ? result : JSON.stringify(result);
    this.#stateManager.setToolCallResult(toolCallId, resultStr, "completed");
  }

  /**
   * Mark a tool call as failed.
   */
  setToolCallError(toolCallId: string, error: string) {
    this.#stateManager.setToolCallResult(toolCallId, error, "error");
  }

  /**
   * Get a pending tool call by ID.
   */
  getToolCall(toolCallId: string): ToolCallInfo | undefined {
    return this.#stateManager.getToolCall(toolCallId);
  }

  /**
   * Send a tool result and continue the conversation.
   * Used after frontend tool execution.
   * @param toolCallId The ID of the tool call this result is for
   * @param result The result from tool execution
   * @param currentMessages The current messages (must include the assistant message with tool calls)
   * @param error Optional error message
   */
  async sendToolResult(toolCallId: string, result: unknown, currentMessages: ChatMessage[], error?: string): Promise<void> {
    // Create a tool message with the result
    const toolMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: "tool",
      content: typeof result === "string" ? result : JSON.stringify(result),
      toolCallId,
      timestamp: new Date(),
    };

    // Update the tool call status via state manager
    const status = error ? "error" : "completed";
    this.#stateManager.setToolCallResult(toolCallId, toolMessage.content, status);

    // Send with the current messages plus the tool result
    // currentMessages must include the assistant message with toolCalls for the LLM to understand
    await this.sendMessage([...currentMessages, toolMessage]);
  }

  /**
   * Reset the client state.
   * Useful when starting a new conversation.
   */
  reset(): void {
    this.#stateManager.reset();
  }
}
