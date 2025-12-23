// Re-export AG-UI types for convenience
export {
  EventType,
  type Tool as AguiTool,
  type ToolMessage,
  type Message as AguiMessage,
} from "@ag-ui/core";

import type { RunAgentInput, BaseEvent, Message } from "@ag-ui/client";
import type { Observable } from "rxjs";

/**
 * Transport interface for agent communication.
 * Enables dependency injection for testability.
 */
export interface AgentTransport {
  /** Run the agent with the given input, returning a stream of events */
  run(input: RunAgentInput): Observable<BaseEvent>;
  /** Set messages for the current run */
  setMessages(messages: Message[]): void;
  /** Abort the current run */
  abortRun(): void;
}

/**
 * Chat message in the conversation.
 * Extends AG-UI Message with additional UI-specific fields.
 */
export interface ChatMessage {
  id: string;
  role: "user" | "assistant" | "tool";
  content: string;
  toolCalls?: ToolCallInfo[];
  /** Required for tool role messages - the ID of the tool call this is responding to */
  toolCallId?: string;
  timestamp: Date;
}

/**
 * Tool call status matching AG-UI events.
 */
export type ToolCallStatus =
  | "pending"    // TOOL_CALL_START received
  | "streaming"  // TOOL_CALL_ARGS being received
  | "executing"  // Frontend tool executing (after TOOL_CALL_END)
  | "completed"  // TOOL_CALL_RESULT received or frontend execution done
  | "error";     // Error occurred

/**
 * Information about a tool call.
 */
export interface ToolCallInfo {
  id: string;
  name: string;
  arguments: string;
  /** Parsed arguments for frontend tool execution */
  parsedArgs?: Record<string, unknown>;
  result?: string;
  status: ToolCallStatus;
}

/**
 * Interrupt information for human-in-the-loop interactions.
 */
export interface InterruptInfo {
  id: string;
  type: "approval" | "input" | "choice" | "custom";
  title: string;
  message: string;
  options?: InterruptOption[];
  inputConfig?: {
    placeholder?: string;
    multiline?: boolean;
  };
  /** AG-UI interrupt payload - contains tool-specific data from server */
  payload?: Record<string, unknown>;
  metadata?: Record<string, unknown>;
}

/**
 * Option for interrupt choices.
 */
export interface InterruptOption {
  value: string;
  label: string;
  variant?: "positive" | "danger" | "default";
}

/**
 * Agent state for displaying progress and status.
 */
export interface AgentState {
  status: "idle" | "thinking" | "executing" | "awaiting_input";
  currentStep?: string;
  progress?: {
    current: number;
    total: number;
    label?: string;
  };
  custom?: Record<string, unknown>;
}

/**
 * Callbacks for AG-UI client events.
 */
export interface AgentClientCallbacks {
  /** Called when a text delta is received */
  onTextDelta?: (delta: string) => void;
  /** Called when text message is complete (content should be accumulated from deltas) */
  onTextEnd?: () => void;
  /** Called when a tool call starts */
  onToolCallStart?: (info: ToolCallInfo) => void;
  /** Called when tool call arguments are complete */
  onToolCallArgsEnd?: (id: string, args: string) => void;
  /** Called when a tool call completes (arguments streamed) */
  onToolCallEnd?: (id: string) => void;
  /** Called when a tool call result is received (backend tool execution) */
  onToolCallResult?: (id: string, result: string) => void;
  /** Called when the run finishes */
  onRunFinished?: (event: RunFinishedEvent) => void;
  /** Called when a state snapshot is received */
  onStateSnapshot?: (state: AgentState) => void;
  /** Called when a state delta is received */
  onStateDelta?: (delta: Partial<AgentState>) => void;
  /** Called when a messages snapshot is received */
  onMessagesSnapshot?: (messages: ChatMessage[]) => void;
  /** Called on error */
  onError?: (error: Error) => void;
}

/**
 * Event fired when a run finishes.
 */
export interface RunFinishedEvent {
  outcome: "success" | "interrupt" | "error";
  interrupt?: InterruptInfo;
  error?: string;
}

// =============================================================================
// Run Lifecycle State Types (Phase 3)
// =============================================================================

/**
 * Discriminated union for run lifecycle state.
 * Enables type-safe state transitions and prepares for session resumption.
 */
export type RunLifecycleState =
  | { status: 'idle' }
  | { status: 'running'; runId: string; threadId: string }
  | { status: 'streaming_text'; runId: string; threadId: string; messageId?: string }
  | { status: 'awaiting_tool_execution'; runId: string; threadId: string; pendingTools: string[] }
  | { status: 'interrupted'; runId: string; threadId: string; interrupt: InterruptInfo }
  | { status: 'error'; runId: string; error: Error };

/**
 * Context for a run, including messages and pending tool calls.
 * This structure supports future session resumption.
 */
export interface RunContext {
  threadId: string;
  runId: string;
  messages: ChatMessage[];
  pendingToolCalls: Map<string, ToolCallInfo>;
  toolCallArgs: Map<string, string>;
}

/**
 * Snapshot of state for session resumption.
 */
export interface RunSnapshot {
  state: RunLifecycleState;
  context?: RunContext;
}
