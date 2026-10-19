import type { UaiStarterPrompt } from "@umbraco-ai/agent";
import type { UaiAgentItem } from "@umbraco-ai/agent-ui";

/**
 * Copilot-specific agent item extending the shared agent item, with each agent's starter prompts for
 * the empty-chat chips. `starterPrompts` is optional so the synthetic "Auto" pseudo-agent (added by the
 * picker when more than one agent is available) can omit it.
 */
export interface UaiCopilotAgentItem extends UaiAgentItem {
    starterPrompts?: UaiStarterPrompt[];
}
