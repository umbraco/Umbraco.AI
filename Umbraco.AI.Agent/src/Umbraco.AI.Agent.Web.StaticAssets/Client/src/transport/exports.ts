/**
 * Public API exports for the transport module.
 */

// Client
export { UaiAgentClient, type UaiAgentClientConfig } from "./uai-agent-client.js";
export { UaiHttpAgent, type UaiHttpAgentConfig, type AGUIStreamRunner } from "./uai-http-agent.js";

// Stored file access. The file endpoint is authenticated, so consumers resolve bytes through the
// configured API client rather than pointing an element at the URL directly.
export { resolveUaiFileObjectUrl, parseUaiFileUrl } from "./uai-file-source.js";

// Domain types
export type {
    UaiChatMessage,
    UaiToolCallStatus,
    UaiToolCallInfo,
    UaiInterruptInfo,
    UaiInterruptOption,
    UaiAgentState,
    UaiFrontendTool,
    UaiInputContent,
    UaiTextInputContent,
    UaiImageInputContent,
    UaiAudioInputContent,
    UaiVideoInputContent,
    UaiDocumentInputContent,
    UaiInputContentSource,
    UaiInputContentDataSource,
    UaiInputContentUrlSource,
} from "./types.js";

export { classifyContentKind } from "./types.js";

// Transport types
export type { AgentTransport, AgentClientCallbacks, RunFinishedEvent } from "./types.js";

// AG-UI re-exports
export { EventType, type AGUITool, type ToolMessage } from "./types.js";

// AG-UI typed events: re-exports of @ag-ui/client's discriminated union members
// (incl. RunFinishedEvent's native outcome + Interrupt as of 0.0.57). RunErrorEvent
// remains a local extension that narrows `code` to UaiErrorCategory.
export type {
    AGUIEvent,
    Interrupt,
    RunFinishedOutcome,
    TextMessageStartEvent,
    TextMessageContentEvent,
    TextMessageEndEvent,
    ToolCallStartEvent,
    ToolCallArgsEvent,
    ToolCallEndEvent,
    ToolCallResultEvent,
    RunErrorEvent,
    StateSnapshotEvent,
    StateDeltaEvent,
    MessagesSnapshotEvent,
    CustomEvent,
} from "./types.js";
