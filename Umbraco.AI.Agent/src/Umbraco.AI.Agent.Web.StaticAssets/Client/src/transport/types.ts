/**
 * Transport layer types for AG-UI protocol communication.
 */

// Re-export AG-UI types for convenience
export { EventType, type Tool as AGUITool, type ToolMessage } from "@ag-ui/core";

import type { RunAgentInput, BaseEvent, Message } from "@ag-ui/client";
import type { Observable } from "rxjs";
import type { Tool } from "@ag-ui/core";

/**
 * Extended AG-UI tool with Umbraco-specific metadata for frontend tools.
 * Used internally by the copilot to attach permission and scope metadata to tools.
 * @public
 */
export interface UaiFrontendTool extends Tool {
    /** Tool scope for permission grouping (e.g., 'entity-write', 'navigation') */
    scope?: string;
    /** Whether the tool performs destructive operations (e.g., delete, publish) */
    isDestructive?: boolean;
}

// =============================================================================
// Domain Types for Agent Communication
// =============================================================================

/**
 * Chat message in the conversation.
 * Extends AG-UI Message with additional UI-specific fields.
 */
export interface UaiChatMessage {
    id: string;
    role: "user" | "assistant" | "tool";
    content: string;
    toolCalls?: UaiToolCallInfo[];
    /** Required for tool role messages - the ID of the tool call this is responding to */
    toolCallId?: string;
    /** Optional agent name for attribution (set when auto mode selects an agent) */
    agentName?: string;
    timestamp: Date;
}

/**
 * Tool call status matching AG-UI events.
 */
export type UaiToolCallStatus =
    | "pending" // TOOL_CALL_START received
    | "streaming" // TOOL_CALL_ARGS being received
    | "awaiting_approval" // Frontend tool waiting for user approval
    | "executing" // Frontend tool executing (after TOOL_CALL_END)
    | "completed" // TOOL_CALL_RESULT received or frontend execution done
    | "error"; // Error occurred

/**
 * Information about a tool call.
 */
export interface UaiToolCallInfo {
    id: string;
    name: string;
    arguments: string;
    /** Parsed arguments for frontend tool execution */
    parsedArgs?: Record<string, unknown>;
    result?: string;
    status: UaiToolCallStatus;
}

/**
 * Interrupt information for human-in-the-loop interactions.
 */
export interface UaiInterruptInfo {
    id: string;
    /** Reason for the interrupt (e.g., "tool_execution" for frontend tools) */
    reason?: string;
    type: "approval" | "input" | "choice" | "custom";
    title: string;
    message: string;
    options?: UaiInterruptOption[];
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
export interface UaiInterruptOption {
    value: string;
    label: string;
    variant?: "positive" | "danger" | "default";
}

/**
 * Agent state for displaying progress and status.
 */
export interface UaiAgentState {
    status: "idle" | "thinking" | "executing" | "awaiting_input";
    currentStep?: string;
    progress?: {
        current: number;
        total: number;
        label?: string;
    };
    custom?: Record<string, unknown>;
}

// =============================================================================
// Transport Interface
// =============================================================================

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
    /** Called when a custom event is received */
    onCustomEvent?: (name: string, value: unknown) => void;
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
// AG-UI Event Types (for type-safe event handling)
// =============================================================================

import { EventType as AGUIEventType } from "@ag-ui/client";

/** Base event type with common fields */
interface TypedBaseEvent {
    type: AGUIEventType;
    rawEvent?: unknown;
}

/** TEXT_MESSAGE_START event */
export interface TextMessageStartEvent extends TypedBaseEvent {
    type: typeof AGUIEventType.TEXT_MESSAGE_START;
    messageId?: string;
}

/** TEXT_MESSAGE_CONTENT event - text delta */
export interface TextMessageContentEvent extends TypedBaseEvent {
    type: typeof AGUIEventType.TEXT_MESSAGE_CONTENT;
    delta: string;
}

/** TEXT_MESSAGE_END event */
export interface TextMessageEndEvent extends TypedBaseEvent {
    type: typeof AGUIEventType.TEXT_MESSAGE_END;
}

/** TOOL_CALL_START event */
export interface ToolCallStartEvent extends TypedBaseEvent {
    type: typeof AGUIEventType.TOOL_CALL_START;
    toolCallId: string;
    toolCallName: string;
}

/** TOOL_CALL_ARGS event - argument delta */
export interface ToolCallArgsEvent extends TypedBaseEvent {
    type: typeof AGUIEventType.TOOL_CALL_ARGS;
    toolCallId: string;
    delta: string;
}

/** TOOL_CALL_END event */
export interface ToolCallEndEvent extends TypedBaseEvent {
    type: typeof AGUIEventType.TOOL_CALL_END;
    toolCallId: string;
}

/** TOOL_CALL_RESULT event - backend tool execution result */
export interface ToolCallResultEvent extends TypedBaseEvent {
    type: typeof AGUIEventType.TOOL_CALL_RESULT;
    toolCallId: string;
    content: string;
}

/** RUN_FINISHED event */
export interface RunFinishedAGUIEvent extends TypedBaseEvent {
    type: typeof AGUIEventType.RUN_FINISHED;
    outcome: string;
    interrupt?: unknown;
    error?: string;
}

/** RUN_ERROR event */
export interface RunErrorEvent extends TypedBaseEvent {
    type: typeof AGUIEventType.RUN_ERROR;
    message: string;
}

/** STATE_SNAPSHOT event */
export interface StateSnapshotEvent extends TypedBaseEvent {
    type: typeof AGUIEventType.STATE_SNAPSHOT;
    state: UaiAgentState;
}

/** STATE_DELTA event */
export interface StateDeltaEvent extends TypedBaseEvent {
    type: typeof AGUIEventType.STATE_DELTA;
    delta: Partial<UaiAgentState>;
}

/** MESSAGES_SNAPSHOT event */
export interface MessagesSnapshotEvent extends TypedBaseEvent {
    type: typeof AGUIEventType.MESSAGES_SNAPSHOT;
    messages: unknown[];
}

/** CUSTOM event */
export interface CustomEvent extends TypedBaseEvent {
    type: typeof AGUIEventType.CUSTOM;
    name: string;
    value: unknown;
}

/** Union of all typed AG-UI events */
export type AGUITypedEvent =
    | TextMessageStartEvent
    | TextMessageContentEvent
    | TextMessageEndEvent
    | ToolCallStartEvent
    | ToolCallArgsEvent
    | ToolCallEndEvent
    | ToolCallResultEvent
    | RunFinishedAGUIEvent
    | RunErrorEvent
    | StateSnapshotEvent
    | StateDeltaEvent
    | MessagesSnapshotEvent
    | CustomEvent;
