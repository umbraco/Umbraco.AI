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
 * Inline-data content source — base64 value with declared MIME type.
 */
export interface UaiInputContentDataSource {
    type: "data";
    value: string;
    mimeType: string;
}

/**
 * URL-based content source — value is a URL the consumer can fetch.
 */
export interface UaiInputContentUrlSource {
    type: "url";
    value: string;
    mimeType?: string;
}

/**
 * Discriminated union of media content sources.
 */
export type UaiInputContentSource = UaiInputContentDataSource | UaiInputContentUrlSource;

interface UaiMediaInputContentBase {
    source: UaiInputContentSource;
    /** Optional metadata bag (e.g., `filename`). */
    metadata?: Record<string, unknown>;
}

/** Image content part (`image/*` mime types). */
export interface UaiImageInputContent extends UaiMediaInputContentBase {
    type: "image";
}

/** Audio content part (`audio/*` mime types). */
export interface UaiAudioInputContent extends UaiMediaInputContentBase {
    type: "audio";
}

/** Video content part (`video/*` mime types). */
export interface UaiVideoInputContent extends UaiMediaInputContentBase {
    type: "video";
}

/** Document content part — catch-all for non-media MIME types (PDF, ZIP, etc.). */
export interface UaiDocumentInputContent extends UaiMediaInputContentBase {
    type: "document";
}

/**
 * Union of all input content types for multimodal messages.
 *
 * AG-UI spec: https://docs.ag-ui.com/concepts. Spec defines five content types
 * (text, image, audio, video, document). The legacy `binary` shape was removed
 * in favour of the typed variants above; document is the catch-all for non-media.
 */
export type UaiInputContent =
    | UaiTextInputContent
    | UaiImageInputContent
    | UaiAudioInputContent
    | UaiVideoInputContent
    | UaiDocumentInputContent;

/**
 * Classify a MIME type into the matching AG-UI content variant. Mirrors the
 * official SDK classifier (`@ag-ui/client` BackwardCompatibility_0_0_47):
 * `image/*` → image, `audio/*` → audio, `video/*` → video, else document.
 */
export function classifyContentKind(mimeType: string | undefined): "image" | "audio" | "video" | "document" {
    if (!mimeType) return "document";
    if (mimeType.startsWith("image/")) return "image";
    if (mimeType.startsWith("audio/")) return "audio";
    if (mimeType.startsWith("video/")) return "video";
    return "document";
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
    /** Reason for the interrupt (e.g., "tool_call" for frontend tools) */
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
    /** Called on error. `code` carries a UaiErrorCategory when the backend classified the failure. */
    onError?: (error: Error, code?: UaiErrorCategory | string) => void;
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
    type StateSnapshotEvent,
    type StateDeltaEvent,
    type MessagesSnapshotEvent,
    type CustomEvent,
    type AGUIEvent,
} from "@ag-ui/client";

// `outcome` (success | interrupt) and the per-interrupt shape are modelled
// natively by the SDK as of @ag-ui/client 0.0.57 — we previously carried local
// extension types (RunFinishedAGUIEvent / AGUIRunOutcome / AGUIInterrupt) to
// compensate for the older schema that only exposed `result?: any`. The SDK's
// RunFinishedEvent is re-exported aliased (AGUIRunFinishedEvent) to avoid
// colliding with the local UI-facing RunFinishedEvent callback type below.
export { type RunFinishedEvent as AGUIRunFinishedEvent } from "@ag-ui/client";
export { type Interrupt, type RunFinishedOutcome } from "@ag-ui/core";

import type { RunErrorEvent as _AGUIRunErrorEvent } from "@ag-ui/client";

/**
 * Normalised error category sent by the backend on RUN_ERROR.
 * Values are produced by `AIProviderErrorCategory.ToString()` server-side.
 */
export type UaiErrorCategory =
    | "Unknown"
    | "Transient"
    | "RateLimited"
    | "Authentication"
    | "InvalidRequest"
    | "NotFound"
    | "Cancelled"
    | "NetworkError";

/**
 * Extended RUN_ERROR event with a backend-classified error category.
 * The SDK's RunErrorEvent only defines `message`; `code` is a Umbraco.AI
 * extension carrying the AIProviderErrorCategory name for retry affordances.
 */
export interface RunErrorEvent extends _AGUIRunErrorEvent {
    /** Provider error category. Absent when the backend didn't classify the error. */
    code?: UaiErrorCategory | string;
}
