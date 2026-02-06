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
} from "./types.js";

// Transport types
export type { AgentTransport, AgentClientCallbacks, RunFinishedEvent } from "./types.js";

// AG-UI re-exports
export { EventType, type AGUITool, type ToolMessage } from "./types.js";

// AG-UI typed events
export type {
    AGUITypedEvent,
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
} from "./types.js";
