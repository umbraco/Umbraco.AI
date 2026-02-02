/**
 * Shared domain types for the Copilot feature.
 *
 * Most types are re-exported from @umbraco-ai/agent for convenience.
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

/**
 * Agent item for copilot agent selector.
 */
export interface UaiCopilotAgentItem {
  id: string;
  name: string;
  alias: string;
}
