import type { ManifestApi } from "@umbraco-cms/backoffice/extension-api";
import type { UaiAgentToolApi, UaiAgentToolApprovalConfig } from "../types/tool.types.js";

/**
 * Manifest for browser-executable frontend tools.
 *
 * Frontend tools execute in the browser and are available to any chat surface.
 * Tool availability per surface is designed to use Umbraco's conditions framework
 * on the manifest. Conditions are not implemented in the first pass -- all registered
 * tools resolve everywhere.
 *
 * Execution concerns only -- does NOT handle rendering.
 * For custom tool UI, see ManifestUaiAgentToolRenderer.
 *
 * @example
 * ```typescript
 * // Simple frontend tool
 * const tool: ManifestUaiAgentFrontendTool = {
 *     type: "uaiAgentFrontendTool",
 *     alias: "My.AgentFrontendTool.GetTime",
 *     meta: {
 *         toolName: "get_current_time",
 *         description: "Returns the current date and time",
 *         parameters: { type: "object", properties: {} },
 *     },
 *     api: () => import("./get-time.api.js"),
 * };
 *
 * // Entity-scoped tool (conditions-ready)
 * const tool: ManifestUaiAgentFrontendTool = {
 *     type: "uaiAgentFrontendTool",
 *     alias: "My.AgentFrontendTool.SetProperty",
 *     meta: {
 *         toolName: "set_property_value",
 *         description: "Sets a property value on the current entity",
 *         parameters: { type: "object", properties: { alias: { type: "string" }, value: {} } },
 *         scope: "entity-write",
 *         isDestructive: false,
 *     },
 *     api: () => import("./set-property.api.js"),
 *     // Future: conditions gate when the tool resolves
 *     // conditions: [{ alias: "Umb.Condition.Context", context: "UAI_ENTITY_CONTEXT" }]
 * };
 *
 * // Tool that requires HITL approval before execution
 * const tool: ManifestUaiAgentFrontendTool = {
 *     type: "uaiAgentFrontendTool",
 *     alias: "My.AgentFrontendTool.DeleteContent",
 *     meta: {
 *         toolName: "delete_content",
 *         description: "Deletes a content item",
 *         parameters: { type: "object", properties: { id: { type: "string" } } },
 *         approval: true,
 *     },
 *     api: () => import("./delete-content.api.js"),
 * };
 * ```
 */
export interface ManifestUaiAgentFrontendTool extends ManifestApi<UaiAgentToolApi> {
    type: "uaiAgentFrontendTool";
    meta: {
        /** Tool name that matches the AG-UI tool call name */
        toolName: string;
        /** Description for LLM (required) */
        description: string;
        /** JSON Schema for tool parameters (required) */
        parameters: Record<string, unknown>;
        /**
         * Tool scope for permission grouping (e.g., 'entity-write', 'navigation').
         * Used to control which agents can access this tool.
         */
        scope?: string;
        /**
         * Whether the tool performs destructive operations.
         * Used for permission filtering.
         */
        isDestructive?: boolean;
        /**
         * HITL approval configuration.
         * When specified, the executor pauses for user approval before invoking the tool.
         * - `true` - Use the default approval element with localized defaults
         * - `{ config }` - Default approval element with custom static config (title, message, etc.)
         */
        approval?: UaiAgentToolApprovalConfig;
    };
}

declare global {
    interface UmbExtensionManifestMap {
        uaiAgentFrontendTool: ManifestUaiAgentFrontendTool;
    }
}
