import type { ManifestElement } from "@umbraco-cms/backoffice/extension-api";
import type { UaiAgentToolElement, UaiAgentToolApprovalConfig } from "../types/tool.types.js";
import { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";
/**
 * Manifest for rendering tool status/results in any chat surface.
 *
 * This manifest type handles the visual representation of tool calls:
 * - Custom UI elements for tool-specific rendering (Generative UI)
 * - Approval configuration for HITL (Human-in-the-Loop) interactions
 * - Icon and label for default status indicators
 *
 * Rendering concerns only -- does NOT handle tool execution.
 * For browser-executable tools, see ManifestUaiAgentFrontendTool.
 *
 * @example
 * ```typescript
 * // Backend tool with custom results UI
 * const renderer: ManifestUaiAgentToolRenderer = {
 *     type: "uaiAgentToolRenderer",
 *     alias: "My.AgentToolRenderer.Search",
 *     meta: { toolName: "search_content", icon: "icon-search" },
 *     element: () => import("./search-results.element.js"),
 * };
 *
 * // Tool with HITL approval
 * const renderer: ManifestUaiAgentToolRenderer = {
 *     type: "uaiAgentToolRenderer",
 *     alias: "My.AgentToolRenderer.SetProperty",
 *     meta: {
 *         toolName: "set_property_value",
 *         label: "Set Property Value",
 *         icon: "icon-edit",
 *         approval: true,
 *     },
 * };
 * ```
 */
export interface ManifestUaiAgentToolRenderer extends ManifestElement<UaiAgentToolElement> {
    type: "uaiAgentToolRenderer";
    kind?: "default";
    meta: {
        /** Tool name that matches the AG-UI tool call name */
        toolName: string;
        /** Display label for the tool */
        label?: string;
        /** Icon to display with the tool */
        icon?: string;
        /**
         * HITL approval configuration.
         * When specified, tool pauses for user approval before execution.
         * - `true` - Use default approval element with localized defaults
         * - `{ elementAlias?, config? }` - Custom approval element and/or config
         */
        approval?: UaiAgentToolApprovalConfig;
    };
}
/**
 * Default kind for uaiAgentToolRenderer extension type.
 *
 * Provides the default tool-status element for tool renderers that don't
 * specify a custom element (Generative UI).
 */
export declare const UAI_AGENT_TOOL_RENDERER_DEFAULT_KIND_MANIFEST: UmbExtensionManifestKind;
declare global {
    interface UmbExtensionManifestMap {
        uaiAgentToolRenderer: ManifestUaiAgentToolRenderer;
    }
}
//# sourceMappingURL=uai-agent-tool-renderer.extension.d.ts.map