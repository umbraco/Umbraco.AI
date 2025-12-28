import {
  type Message,
  type BaseEvent,
  EventType as AguiEventType,
  transformChunks,
} from "@ag-ui/client";
import { UaiHttpAgent } from "./uai-http-agent.js";
import { RunStateManager, type StateChangeListener } from "./run-state-manager.js";
import type { ChatMessage, ToolCallInfo, InterruptInfo } from "../core/types.js";
import type {
  AgentClientCallbacks,
  AguiTool,
  AgentTransport,
  RunLifecycleState,
  TextMessageStartEvent,
  TextMessageContentEvent,
  ToolCallStartEvent,
  ToolCallArgsEvent,
  ToolCallEndEvent,
  ToolCallResultEvent,
  RunFinishedAguiEvent,
  RunErrorEvent,
  StateSnapshotEvent,
  StateDeltaEvent,
  MessagesSnapshotEvent,
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
  #stateManager: RunStateManager;

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
   * Update the callbacks dynamically.
   * Available for external use to change event handlers after construction.
   * @param callbacks The new set of callbacks to use
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
   * Convert ChatMessage to AG-UI Message format.
   */
  static #toAguiMessage(m: ChatMessage): Message {
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
  async sendMessage(messages: ChatMessage[], tools?: AguiTool[]): Promise<void> {
    const threadId = crypto.randomUUID();
    const runId = crypto.randomUUID();

    // Initialize state manager for this run
    this.#stateManager.startRun(runId, threadId, messages);

    // Set messages on the agent before running
    const convertedMessages = messages.map((m) => UaiAgentClient.#toAguiMessage(m));
    this.#transport.setMessages(convertedMessages);

    // Use tools passed in directly
    const allTools = tools ?? [];

    try {
      // Subscribe to the transport's event stream
      // Apply transformChunks to convert CHUNK events → START/CONTENT/END events
      this.#transport.run({
        threadId,
        runId,
        messages: convertedMessages,
        tools: allTools,
        context: [],
      }).pipe(
        transformChunks(false)
      ).subscribe({
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
   * @param interruptResponse The user's response to the interrupt
   * @param tools Optional tools to include (should be the same as the original run)
   */
  async resumeRun(interruptResponse: string, tools?: AguiTool[]): Promise<void> {
    const userMessage: ChatMessage = {
      id: crypto.randomUUID(),
      role: "user",
      content: interruptResponse,
      timestamp: new Date(),
    };

    await this.sendMessage([...this.#stateManager.messages, userMessage], tools);
  }

  /**
   * Handle incoming AG-UI events.
   * Uses typed event interfaces for type safety.
   */
  #handleEvent(event: BaseEvent) {
    switch (event.type) {
      case AguiEventType.TEXT_MESSAGE_START: {
        const messageId = (event as TextMessageStartEvent).messageId;
        this.#stateManager.startStreaming(messageId);
        // Call onTextStart for multi-block UI support
        if (messageId) {
          this.#callbacks.onTextStart?.(messageId);
        }
        break;
      }

      case AguiEventType.TEXT_MESSAGE_CONTENT:
        this.#callbacks.onTextDelta?.((event as TextMessageContentEvent).delta);
        break;

      case AguiEventType.TEXT_MESSAGE_END:
        this.#stateManager.endStreaming();
        this.#callbacks.onTextEnd?.();
        break;

      case AguiEventType.TOOL_CALL_START:
        this.#handleToolCallStart(event as ToolCallStartEvent);
        break;

      case AguiEventType.TOOL_CALL_ARGS:
        this.#handleToolCallArgs(event as ToolCallArgsEvent);
        break;

      case AguiEventType.TOOL_CALL_END:
        this.#handleToolCallEnd(event as ToolCallEndEvent);
        break;

      case AguiEventType.TOOL_CALL_RESULT:
        this.#handleToolCallResult(event as ToolCallResultEvent);
        break;

      case AguiEventType.RUN_FINISHED:
        this.#handleRunFinished(event as RunFinishedAguiEvent);
        break;

      case AguiEventType.RUN_ERROR: {
        const err = new Error((event as RunErrorEvent).message);
        this.#stateManager.setError(err);
        this.#callbacks.onError?.(err);
        break;
      }

      case AguiEventType.STATE_SNAPSHOT:
        this.#callbacks.onStateSnapshot?.((event as StateSnapshotEvent).state);
        break;

      case AguiEventType.STATE_DELTA:
        this.#callbacks.onStateDelta?.((event as StateDeltaEvent).delta);
        break;

      case AguiEventType.MESSAGES_SNAPSHOT:
        this.#handleMessagesSnapshot(event as MessagesSnapshotEvent);
        break;
    }
  }

  #handleToolCallStart(event: ToolCallStartEvent) {
    const toolCall: ToolCallInfo = {
      id: event.toolCallId,
      name: event.toolCallName,
      arguments: "",
      status: "pending",
    };

    this.#stateManager.addToolCall(toolCall);
    this.#callbacks.onToolCallStart?.(toolCall);
  }

  #handleToolCallArgs(event: ToolCallArgsEvent) {
    this.#stateManager.appendToolCallArgs(event.toolCallId, event.delta);
  }

  #handleToolCallEnd(event: ToolCallEndEvent) {
    const toolCallId = event.toolCallId;
    const args = this.#stateManager.finalizeToolCallArgs(toolCallId);

    if (args !== undefined) {
      // Status stays "pending" for backend tools (waiting for TOOL_CALL_RESULT)
      // Frontend tools will set status to "executing" when they execute
      this.#callbacks.onToolCallArgsEnd?.(toolCallId, args);
      this.#callbacks.onToolCallEnd?.(toolCallId);
    }
  }

  #handleToolCallResult(event: ToolCallResultEvent) {
    const toolCallId = event.toolCallId;
    const content = event.content;

    this.#stateManager.setToolCallResult(toolCallId, content, "completed");
    this.#callbacks.onToolCallResult?.(toolCallId, content);
  }

  #handleRunFinished(event: RunFinishedAguiEvent) {
    // Normalize outcome to lowercase for case-insensitive comparison
    // Backend sends PascalCase (e.g., "Interrupt") but we use lowercase
    const outcome = (event.outcome ?? "").toLowerCase();

    if (outcome === "interrupt") {
      const interrupt = UaiAgentClient.#parseInterrupt(event.interrupt);
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

  #handleMessagesSnapshot(event: MessagesSnapshotEvent) {
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

  static #parseInterrupt(raw: unknown): InterruptInfo {
    const data = raw as Record<string, unknown>;

    return {
      id: (data.id as string) ?? crypto.randomUUID(),
      reason: data.reason as string | undefined,
      type: (data.type as InterruptInfo["type"]) ?? "custom",
      title: (data.title as string) ?? "Action Required",
      message: (data.message as string) ?? "",
      options: data.options as InterruptInfo["options"],
      inputConfig: data.inputConfig as InterruptInfo["inputConfig"],
      payload: data.payload as Record<string, unknown>,
      metadata: data.metadata as Record<string, unknown>,
    };
  }

  /**
   * Get a pending tool call by ID.
   */
  getToolCall(toolCallId: string): ToolCallInfo | undefined {
    return this.#stateManager.getToolCall(toolCallId);
  }

  /**
   * Signal that we're awaiting frontend tool execution.
   * Called by the consumer when frontend tools need to be executed.
   * @param pendingToolIds IDs of tools awaiting execution
   */
  awaitToolExecution(pendingToolIds: string[]): void {
    this.#stateManager.awaitToolExecution(pendingToolIds);
  }

  /**
   * Reset the client state.
   * Useful when starting a new conversation.
   */
  reset(): void {
    this.#stateManager.reset();
  }
}
