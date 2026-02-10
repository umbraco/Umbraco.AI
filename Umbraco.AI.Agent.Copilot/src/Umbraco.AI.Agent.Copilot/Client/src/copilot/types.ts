/**
 * Shared domain types for the Copilot feature.
 *
 * Most types are re-exported from @umbraco-ai/agent-ui and @umbraco-ai/agent for convenience.
 * Only copilot-specific types are defined here.
 */

// Re-export transport types from @umbraco-ai/agent
export type {
    UaiChatMessage,
    UaiToolCallStatus,
    UaiToolCallInfo,
    UaiInterruptInfo,
    UaiInterruptOption,
    UaiAgentState,
} from "@umbraco-ai/agent";

// Re-export shared agent item type from agent-ui
export type { UaiAgentItem } from "@umbraco-ai/agent-ui";

/**
 * Copilot-specific agent item extending the shared agent item.
 * Currently identical to UaiAgentItem but allows copilot-specific extensions in the future.
 */
export type UaiCopilotAgentItem = import("@umbraco-ai/agent-ui").UaiAgentItem;
