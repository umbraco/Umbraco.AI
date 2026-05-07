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
// Multimodal Content Types (AG-UI Multimodal Messages Draft)
// =============================================================================

/**
 * Text content part for multimodal messages.
 */
export interface UaiTextInputContent {
    type: "text";
    text: string;
}

/**
 * Binary content part for multimodal messages (images, PDFs, etc.).
 * At least one of data, url, or id must be provided.
 */
export interface UaiBinaryInputContent {
    type: "binary";
    mimeType: string;
    /** Base64-encoded binary data (initial upload) */
    data?: string;
    /** URL where the binary content can be retrieved */
    url?: string;
    /** Server-side reference ID (after snapshot, for subsequent turns) */
    id?: string;
    /** Original filename */
    filename?: string;
}

/**
 * Union of all input content types for multimodal messages.
 */
export type UaiInputContent = UaiTextInputContent | UaiBinaryInputContent;

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
    /** Multimodal content parts (text + binary). When present, content is a text summary. */
    contentParts?: UaiInputContent[];
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
// AG-UI Event Types (re-exported from @ag-ui/client for downstream consumers)
// =============================================================================

export {
    type TextMessageStartEvent,
    type TextMessageContentEvent,
    type TextMessageEndEvent,
    type ToolCallStartEvent,
    type ToolCallArgsEvent,
    type ToolCallEndEvent,
    type ToolCallResultEvent,
    type RunErrorEvent,
    type StateSnapshotEvent,
    type StateDeltaEvent,
    type MessagesSnapshotEvent,
    type CustomEvent,
    type AGUIEvent,
} from "@ag-ui/client";

import type { RunFinishedEvent as AGUIRunFinishedEvent } from "@ag-ui/client";

/**
 * AG-UI interrupt object — see https://docs.ag-ui.com/concepts/interrupts.
 *
 * The server emits one entry per pending interrupt inside
 * `RunFinishedEvent.outcome.interrupts` when the run pauses for human input.
 *
 * REMOVE WHEN: `@ag-ui/client` updates its Zod schema to model the
 * `outcome` discriminated union (currently the SDK has not caught up to the
 * published spec — see RunFinishedAGUIEvent below).
 */
export interface AGUIInterrupt {
    id: string;
    reason: string;
    message?: string;
    toolCallId?: string;
    responseSchema?: unknown;
    expiresAt?: string;
    metadata?: Record<string, unknown>;
}

/**
 * Discriminated union for the `outcome` field on a RUN_FINISHED event.
 *
 * REMOVE WHEN: `@ag-ui/client` updates its Zod `RunFinishedEventSchema` to
 * model `outcome`. As of 0.0.53 the SDK schema only exposes
 * `result?: any`; the `outcome` field still rides on the wire via Zod's
 * `passthrough`, but typing it requires this local extension.
 */
export type AGUIRunOutcome =
    | { type: "success" }
    | { type: "interrupt"; interrupts: AGUIInterrupt[] };

/**
 * Spec-shaped extension of AG-UI's RunFinishedEvent that adds `outcome`.
 *
 * REMOVE WHEN: `@ag-ui/client` updates its Zod schema to include the
 * `outcome` field (then `outcome` will be inferred natively and downstream
 * consumers can import the SDK's RunFinishedEvent directly).
 */
export interface RunFinishedAGUIEvent extends AGUIRunFinishedEvent {
    outcome: AGUIRunOutcome;
}
