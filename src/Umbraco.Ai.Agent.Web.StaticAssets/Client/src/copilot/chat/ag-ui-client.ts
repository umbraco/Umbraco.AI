import { type Message } from "@ag-ui/client";
import { UaiHttpAgent } from "./uai-http-agent.js";
import type {
  ChatMessage,
  AgentClientCallbacks,
  ToolCallInfo,
  AgentState,
  InterruptInfo,
  EventType,
  AgUiTool,
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
  #agent: UaiHttpAgent;
  #callbacks: AgentClientCallbacks;
  #messages: ChatMessage[] = [];
  #pendingToolCalls: Map<string, ToolCallInfo> = new Map();
  #toolCallArgs: Map<string, string> = new Map();
  #frontendTools: AgUiTool[] = [];

  constructor(config: UaiAgentClientConfig, callbacks: AgentClientCallbacks = {}) {
    this.#callbacks = callbacks;
    this.#agent = new UaiHttpAgent({ agentId: config.agentId });
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
      return {
        id: m.id,
        role: "assistant" as const,
        content: m.content,
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
    this.#agent.setMessages(messages.map((m) => this.#toAgUiMessage(m)));

    // Merge frontend tools with any additional tools passed in
    const allTools = [...this.#frontendTools, ...(tools ?? [])];

    try {
      await this.#agent.runAgent(
        {
          runId: crypto.randomUUID(),
          // Only include tools if there are any
          ...(allTools.length > 0 && { tools: allTools }),
        },
        {
          onEvent: (params) => {
            this.#handleEvent(params.event as { type: string; [key: string]: unknown });
          },
        }
      );
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
  #handleEvent(event: { type: string; [key: string]: unknown }) {
    switch (event.type as EventType) {
      case "TEXT_MESSAGE_START":
        // Message started - nothing to do
        break;

      case "TEXT_MESSAGE_CONTENT":
        this.#callbacks.onTextDelta?.(event.delta as string);
        break;

      case "TEXT_MESSAGE_END":
        this.#callbacks.onTextEnd?.();
        break;

      case "TOOL_CALL_START":
        this.#handleToolCallStart(event);
        break;

      case "TOOL_CALL_ARGS":
        this.#handleToolCallArgs(event);
        break;

      case "TOOL_CALL_END":
        this.#handleToolCallEnd(event);
        break;

      case "TOOL_CALL_RESULT":
        this.#handleToolCallResult(event);
        break;

      case "RUN_FINISHED":
        this.#handleRunFinished(event);
        break;

      case "RUN_ERROR":
        this.#callbacks.onError?.(new Error(event.message as string));
        break;

      case "STATE_SNAPSHOT":
        this.#callbacks.onStateSnapshot?.(event.state as AgentState);
        break;

      case "STATE_DELTA":
        this.#callbacks.onStateDelta?.(event.delta as Partial<AgentState>);
        break;

      case "MESSAGES_SNAPSHOT":
        this.#handleMessagesSnapshot(event);
        break;
    }
  }

  #handleToolCallStart(event: { [key: string]: unknown }) {
    const toolCall: ToolCallInfo = {
      id: event.toolCallId as string,
      name: event.toolCallName as string,
      arguments: "",
      status: "pending",
    };

    this.#pendingToolCalls.set(toolCall.id, toolCall);
    this.#toolCallArgs.set(toolCall.id, "");
    this.#callbacks.onToolCallStart?.(toolCall);
  }

  #handleToolCallArgs(event: { [key: string]: unknown }) {
    const toolCallId = event.toolCallId as string;
    const delta = event.delta as string;
    const currentArgs = this.#toolCallArgs.get(toolCallId) ?? "";
    this.#toolCallArgs.set(toolCallId, currentArgs + delta);
  }

  #handleToolCallEnd(event: { [key: string]: unknown }) {
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

  #handleToolCallResult(event: { [key: string]: unknown }) {
    const toolCallId = event.toolCallId as string;
    const content = event.content as string;

    const toolCall = this.#pendingToolCalls.get(toolCallId);
    if (toolCall) {
      toolCall.result = content;
      toolCall.status = "completed";
    }

    this.#callbacks.onToolCallResult?.(toolCallId, content);
  }

  #handleRunFinished(event: { [key: string]: unknown }) {
    const outcome = event.outcome as string;

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

  #handleMessagesSnapshot(event: { [key: string]: unknown }) {
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
   */
  async sendToolResult(toolCallId: string, result: unknown, error?: string): Promise<void> {
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

    // Send with the tool message included
    await this.sendMessage([...this.#messages, toolMessage]);
  }
}
