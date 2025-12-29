/**
 * Transport layer types for AG-UI protocol communication.
 */

// Re-export AG-UI types for convenience
export {
  EventType,
  type Tool as AguiTool,
  type ToolMessage,
  type Message as AguiMessage,
} from "@ag-ui/core";

import type { RunAgentInput, BaseEvent, Message } from "@ag-ui/client";
import type { Observable } from "rxjs";
import type { UaiChatMessage, UaiToolCallInfo, UaiInterruptInfo, UaiAgentState } from "../core/types.js";

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
 * Callbacks for AG-UI client events.
 */
export interface AgentClientCallbacks {
  /** Called when a new text message starts (with messageId for multi-block UI) */
  onTextStart?: (messageId: string) => void;
  /** Called when a text delta is received */
  onTextDelta?: (delta: string) => void;
  /** Called when text message is complete (content should be accumulated from deltas) */
  onTextEnd?: () => void;
  /** Called when a tool call starts */
  onToolCallStart?: (info: UaiToolCallInfo) => void;
  /** Called when tool call arguments are complete */
  onToolCallArgsEnd?: (id: string, args: string) => void;
  /** Called when a tool call completes (arguments streamed) */
  onToolCallEnd?: (id: string) => void;
  /** Called when a tool call result is received (backend tool execution) */
  onToolCallResult?: (id: string, result: string) => void;
  /** Called when the run finishes */
  onRunFinished?: (event: RunFinishedEvent) => void;
  /** Called when a state snapshot is received */
  onStateSnapshot?: (state: UaiAgentState) => void;
  /** Called when a state delta is received */
  onStateDelta?: (delta: Partial<UaiAgentState>) => void;
  /** Called when a messages snapshot is received */
  onMessagesSnapshot?: (messages: UaiChatMessage[]) => void;
  /** Called on error */
  onError?: (error: Error) => void;
}

/**
 * Event fired when a run finishes.
 */
export interface RunFinishedEvent {
  outcome: "success" | "interrupt" | "error";
  interrupt?: UaiInterruptInfo;
  error?: string;
}

// =============================================================================
// Run Lifecycle State Types
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
  | { status: 'interrupted'; runId: string; threadId: string; interrupt: UaiInterruptInfo }
  | { status: 'error'; runId: string; error: Error };

/**
 * Context for a run, including messages and pending tool calls.
 * This structure supports future session resumption.
 */
export interface RunContext {
  threadId: string;
  runId: string;
  messages: UaiChatMessage[];
  pendingToolCalls: Map<string, UaiToolCallInfo>;
  toolCallArgs: Map<string, string>;
}

/**
 * Snapshot of state for session resumption.
 */
export interface RunSnapshot {
  state: RunLifecycleState;
  context?: RunContext;
}

// =============================================================================
// AG-UI Event Types (for type-safe event handling)
// =============================================================================

import { EventType as AguiEventType } from "@ag-ui/client";

/** Base event type with common fields */
interface TypedBaseEvent {
  type: AguiEventType;
  rawEvent?: unknown;
}

/** TEXT_MESSAGE_START event */
export interface TextMessageStartEvent extends TypedBaseEvent {
  type: typeof AguiEventType.TEXT_MESSAGE_START;
  messageId?: string;
}

/** TEXT_MESSAGE_CONTENT event - text delta */
export interface TextMessageContentEvent extends TypedBaseEvent {
  type: typeof AguiEventType.TEXT_MESSAGE_CONTENT;
  delta: string;
}

/** TEXT_MESSAGE_END event */
export interface TextMessageEndEvent extends TypedBaseEvent {
  type: typeof AguiEventType.TEXT_MESSAGE_END;
}

/** TOOL_CALL_START event */
export interface ToolCallStartEvent extends TypedBaseEvent {
  type: typeof AguiEventType.TOOL_CALL_START;
  toolCallId: string;
  toolCallName: string;
}

/** TOOL_CALL_ARGS event - argument delta */
export interface ToolCallArgsEvent extends TypedBaseEvent {
  type: typeof AguiEventType.TOOL_CALL_ARGS;
  toolCallId: string;
  delta: string;
}

/** TOOL_CALL_END event */
export interface ToolCallEndEvent extends TypedBaseEvent {
  type: typeof AguiEventType.TOOL_CALL_END;
  toolCallId: string;
}

/** TOOL_CALL_RESULT event - backend tool execution result */
export interface ToolCallResultEvent extends TypedBaseEvent {
  type: typeof AguiEventType.TOOL_CALL_RESULT;
  toolCallId: string;
  content: string;
}

/** RUN_FINISHED event */
export interface RunFinishedAguiEvent extends TypedBaseEvent {
  type: typeof AguiEventType.RUN_FINISHED;
  outcome: string;
  interrupt?: unknown;
  error?: string;
}

/** RUN_ERROR event */
export interface RunErrorEvent extends TypedBaseEvent {
  type: typeof AguiEventType.RUN_ERROR;
  message: string;
}

/** STATE_SNAPSHOT event */
export interface StateSnapshotEvent extends TypedBaseEvent {
  type: typeof AguiEventType.STATE_SNAPSHOT;
  state: UaiAgentState;
}

/** STATE_DELTA event */
export interface StateDeltaEvent extends TypedBaseEvent {
  type: typeof AguiEventType.STATE_DELTA;
  delta: Partial<UaiAgentState>;
}

/** MESSAGES_SNAPSHOT event */
export interface MessagesSnapshotEvent extends TypedBaseEvent {
  type: typeof AguiEventType.MESSAGES_SNAPSHOT;
  messages: unknown[];
}

/** Union of all typed AG-UI events */
export type AguiTypedEvent =
  | TextMessageStartEvent
  | TextMessageContentEvent
  | TextMessageEndEvent
  | ToolCallStartEvent
  | ToolCallArgsEvent
  | ToolCallEndEvent
  | ToolCallResultEvent
  | RunFinishedAguiEvent
  | RunErrorEvent
  | StateSnapshotEvent
  | StateDeltaEvent
  | MessagesSnapshotEvent;
