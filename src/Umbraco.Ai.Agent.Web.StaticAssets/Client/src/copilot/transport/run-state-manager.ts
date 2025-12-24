import type { ChatMessage, ToolCallInfo, InterruptInfo } from "../core/types.js";
import type { RunLifecycleState, RunContext, RunSnapshot } from "./types.js";

/**
 * Callback type for state change listeners.
 */
export type StateChangeListener = (state: RunLifecycleState) => void;

/**
 * Manages the lifecycle state of an agent run.
 * 
 * Centralizes state management that was previously scattered across
 * UaiAgentClient and the consumer component. Provides:
 * - Type-safe state transitions
 * - State change subscriptions
 * - Snapshot/restore for future session resumption
 */
export class RunStateManager {
  #state: RunLifecycleState = { status: 'idle' };
  #context?: RunContext;
  #listeners: Set<StateChangeListener> = new Set();

  /**
   * Get the current run lifecycle state.
   */
  get state(): RunLifecycleState {
    return this.#state;
  }

  /**
   * Get the current run context (messages, tool calls, etc).
   */
  get context(): RunContext | undefined {
    return this.#context;
  }

  /**
   * Check if currently in an active run.
   */
  get isRunning(): boolean {
    return this.#state.status !== 'idle' && this.#state.status !== 'error';
  }

  /**
   * Transition to a new state.
   * Notifies all listeners of the state change.
   */
  transition(newState: RunLifecycleState): void {
    this.#state = newState;
    this.#notifyListeners();
  }

  /**
   * Start a new run.
   */
  startRun(runId: string, threadId: string, messages: ChatMessage[]): void {
    this.#context = {
      threadId,
      runId,
      messages: [...messages],
      pendingToolCalls: new Map(),
      toolCallArgs: new Map(),
    };
    this.transition({ status: 'running', runId, threadId });
  }

  /**
   * Transition to streaming text state.
   */
  startStreaming(messageId?: string): void {
    if (this.#state.status === 'running' || this.#state.status === 'streaming_text') {
      const { runId, threadId } = this.#state;
      this.transition({ status: 'streaming_text', runId, threadId, messageId });
    }
  }

  /**
   * End streaming and transition back to running state.
   */
  endStreaming(): void {
    if (this.#state.status === 'streaming_text') {
      const { runId, threadId } = this.#state;
      this.transition({ status: 'running', runId, threadId });
    }
  }

  /**
   * Add a pending tool call.
   */
  addToolCall(toolCall: ToolCallInfo): void {
    if (this.#context) {
      this.#context.pendingToolCalls.set(toolCall.id, toolCall);
      this.#context.toolCallArgs.set(toolCall.id, '');
    }
  }

  /**
   * Append to tool call arguments being streamed.
   */
  appendToolCallArgs(toolCallId: string, delta: string): void {
    if (this.#context) {
      const current = this.#context.toolCallArgs.get(toolCallId) ?? '';
      this.#context.toolCallArgs.set(toolCallId, current + delta);
    }
  }

  /**
   * Finalize tool call arguments.
   */
  finalizeToolCallArgs(toolCallId: string): string | undefined {
    if (this.#context) {
      const args = this.#context.toolCallArgs.get(toolCallId);
      const toolCall = this.#context.pendingToolCalls.get(toolCallId);
      if (toolCall && args !== undefined) {
        toolCall.arguments = args;
      }
      return args;
    }
    return undefined;
  }

  /**
   * Set tool call result.
   */
  setToolCallResult(toolCallId: string, result: string, status: 'completed' | 'error' = 'completed'): void {
    if (this.#context) {
      const toolCall = this.#context.pendingToolCalls.get(toolCallId);
      if (toolCall) {
        toolCall.result = result;
        toolCall.status = status;
      }
    }
  }

  /**
   * Get a pending tool call by ID.
   */
  getToolCall(toolCallId: string): ToolCallInfo | undefined {
    return this.#context?.pendingToolCalls.get(toolCallId);
  }

  /**
   * Transition to awaiting tool execution state.
   */
  awaitToolExecution(pendingToolIds: string[]): void {
    if (this.#state.status !== 'idle') {
      const runId = 'runId' in this.#state ? this.#state.runId : '';
      const threadId = 'threadId' in this.#state ? this.#state.threadId : '';
      this.transition({ 
        status: 'awaiting_tool_execution', 
        runId, 
        threadId, 
        pendingTools: pendingToolIds 
      });
    }
  }

  /**
   * Transition to interrupted state.
   */
  interrupt(interrupt: InterruptInfo): void {
    if (this.#state.status !== 'idle') {
      const runId = 'runId' in this.#state ? this.#state.runId : '';
      const threadId = 'threadId' in this.#state ? this.#state.threadId : '';
      this.transition({ status: 'interrupted', runId, threadId, interrupt });
    }
  }

  /**
   * Complete the run successfully.
   */
  complete(): void {
    this.transition({ status: 'idle' });
  }

  /**
   * Set error state.
   */
  setError(error: Error): void {
    const runId = 'runId' in this.#state ? this.#state.runId : '';
    this.transition({ status: 'error', runId, error });
  }

  /**
   * Reset to idle state.
   */
  reset(): void {
    this.#context = undefined;
    this.transition({ status: 'idle' });
  }

  /**
   * Update messages in the context.
   */
  setMessages(messages: ChatMessage[]): void {
    if (this.#context) {
      this.#context.messages = [...messages];
    }
  }

  /**
   * Get current messages.
   */
  get messages(): ChatMessage[] {
    return this.#context?.messages ? [...this.#context.messages] : [];
  }

  /**
   * Subscribe to state changes.
   * @returns Unsubscribe function
   */
  subscribe(listener: StateChangeListener): () => void {
    this.#listeners.add(listener);
    return () => {
      this.#listeners.delete(listener);
    };
  }

  /**
   * Create a snapshot for session resumption.
   */
  toSnapshot(): RunSnapshot {
    return {
      state: { ...this.#state },
      context: this.#context ? {
        ...this.#context,
        messages: [...this.#context.messages],
        pendingToolCalls: new Map(this.#context.pendingToolCalls),
        toolCallArgs: new Map(this.#context.toolCallArgs),
      } : undefined,
    };
  }

  /**
   * Restore from a snapshot.
   */
  fromSnapshot(snapshot: RunSnapshot): void {
    this.#state = snapshot.state;
    this.#context = snapshot.context ? {
      ...snapshot.context,
      messages: [...snapshot.context.messages],
      pendingToolCalls: new Map(snapshot.context.pendingToolCalls),
      toolCallArgs: new Map(snapshot.context.toolCallArgs),
    } : undefined;
    this.#notifyListeners();
  }

  /**
   * Notify all listeners of state change.
   */
  #notifyListeners(): void {
    for (const listener of this.#listeners) {
      listener(this.#state);
    }
  }
}
