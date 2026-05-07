/**
 * Public API exports for the transport module.
 */

// Client
export { UaiAgentClient, type UaiAgentClientConfig } from "./uai-agent-client.js";
export { UaiHttpAgent, type UaiHttpAgentConfig } from "./uai-http-agent.js";

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

// AG-UI typed events (re-exports of @ag-ui/client's discriminated union members,
// plus locally-extended types for spec features the SDK schema hasn't caught up with).
export type {
    AGUIEvent,
    AGUIInterrupt,
    AGUIRunOutcome,
    TextMessageStartEvent,
    TextMessageContentEvent,
    TextMessageEndEvent,
    ToolCallStartEvent,
    ToolCallArgsEvent,
    ToolCallEndEvent,
    ToolCallResultEvent,
    RunFinishedAGUIEvent,
    RunErrorEvent,
    StateSnapshotEvent,
    StateDeltaEvent,
    MessagesSnapshotEvent,
    CustomEvent,
} from "./types.js";
