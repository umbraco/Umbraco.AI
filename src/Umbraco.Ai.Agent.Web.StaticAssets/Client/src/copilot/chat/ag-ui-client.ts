import {
  type Message,
  type BaseEvent,
  EventType as AguiEventType,
} from "@ag-ui/client";
import { UaiHttpAgent } from "./uai-http-agent.js";
import type {
  ChatMessage,
  AgentClientCallbacks,
  ToolCallInfo,
  AgentState,
  InterruptInfo,
  AgUiTool,
  AgentTransport,
} from "./types.js";

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
  #messages: ChatMessage[] = [];
  #pendingToolCalls: Map<string, ToolCallInfo> = new Map();
  #toolCallArgs: Map<string, string> = new Map();
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
    return [...this.#messages];
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
    this.#messages = messages;
    this.#pendingToolCalls.clear();
    this.#toolCallArgs.clear();

    // Set messages on the agent before running
    const convertedMessages = messages.map((m) => this.#toAgUiMessage(m));
    this.#transport.setMessages(convertedMessages);

    // Merge frontend tools with any additional tools passed in
    const allTools = [...this.#frontendTools, ...(tools ?? [])];

    try {
      // Subscribe to the transport's event stream
      this.#transport.run({
        threadId: crypto.randomUUID(),
        runId: crypto.randomUUID(),
        messages: convertedMessages,
        tools: allTools,
        context: [],
      }).subscribe({
        next: (event) => {
          this.#handleEvent(event);
        },
        error: (error) => {
          this.#callbacks.onError?.(error instanceof Error ? error : new Error(String(error)));
        },
        complete: () => {
          // Stream completed
        },
      });
    } catch (error) {
      this.#callbacks.onError?.(error instanceof Error ? error : new Error(String(error)));
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

    await this.sendMessage([...this.#messages, userMessage]);
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

    this.#pendingToolCalls.set(toolCall.id, toolCall);
    this.#toolCallArgs.set(toolCall.id, "");
    this.#callbacks.onToolCallStart?.(toolCall);
  }

  #handleToolCallArgs(event: BaseEvent & { toolCallId: string; delta: string }) {
    const toolCallId = event.toolCallId;
    const delta = event.delta;
    const currentArgs = this.#toolCallArgs.get(toolCallId) ?? "";
    this.#toolCallArgs.set(toolCallId, currentArgs + delta);
  }

  #handleToolCallEnd(event: BaseEvent & { toolCallId: string }) {
    const toolCallId = event.toolCallId as string;
    const toolCall = this.#pendingToolCalls.get(toolCallId);

    if (toolCall) {
      const args = this.#toolCallArgs.get(toolCallId) ?? "";
      toolCall.arguments = args;
      // Status stays "pending" for backend tools (waiting for TOOL_CALL_RESULT)
      // Frontend tools will set status to "executing" when they execute
      this.#callbacks.onToolCallArgsEnd?.(toolCallId, args);
      this.#callbacks.onToolCallEnd?.(toolCallId);
    }
  }

  #handleToolCallResult(event: BaseEvent & { toolCallId: string; content: string }) {
    const toolCallId = event.toolCallId as string;
    const content = event.content;

    const toolCall = this.#pendingToolCalls.get(toolCallId);
    if (toolCall) {
      toolCall.result = content;
      toolCall.status = "completed";
    }

    this.#callbacks.onToolCallResult?.(toolCallId, content);
  }

  #handleRunFinished(event: BaseEvent & { outcome: string; interrupt?: unknown; error?: string }) {
    const outcome = event.outcome;

    if (outcome === "interrupt") {
      const interrupt = this.#parseInterrupt(event.interrupt);
      this.#callbacks.onRunFinished?.({
        outcome: "interrupt",
        interrupt,
      });
    } else if (outcome === "error") {
      this.#callbacks.onRunFinished?.({
        outcome: "error",
        error: event.error as string,
      });
    } else {
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

    this.#messages = messages;
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
    const toolCall = this.#pendingToolCalls.get(toolCallId);
    if (toolCall) {
      toolCall.result = JSON.stringify(result);
      toolCall.status = "completed";
    }
  }

  /**
   * Mark a tool call as failed.
   */
  setToolCallError(toolCallId: string, error: string) {
    const toolCall = this.#pendingToolCalls.get(toolCallId);
    if (toolCall) {
      toolCall.result = error;
      toolCall.status = "error";
    }
  }

  /**
   * Get a pending tool call by ID.
   */
  getToolCall(toolCallId: string): ToolCallInfo | undefined {
    return this.#pendingToolCalls.get(toolCallId);
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

    // Update the tool call status
    const toolCall = this.#pendingToolCalls.get(toolCallId);
    if (toolCall) {
      toolCall.result = toolMessage.content;
      toolCall.status = error ? "error" : "completed";
    }

    // Send with the current messages plus the tool result
    // currentMessages must include the assistant message with toolCalls for the LLM to understand
    await this.sendMessage([...currentMessages, toolMessage]);
  }
}
