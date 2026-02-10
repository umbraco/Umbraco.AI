/**
 * Public API exports for the copilot module.
 */

// Tool extension types (re-exported from agent-ui)
export * from "./tools/exports.js";

// Approval extension types (re-exported from agent-ui)
export * from "./approval/exports.js";

// Copilot types
export type { UaiCopilotAgentItem } from "./types.js";

// Copilot context
export { UaiCopilotContext, UAI_COPILOT_CONTEXT } from "./copilot.context.js";
