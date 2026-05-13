import type { ManifestElement } from "@umbraco-cms/backoffice/extension-api";
import type { UaiAgentToolElement } from "../types/tool.types.js";
import { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";

/**
 * Manifest for rendering tool status/results in any chat surface.
 *
 * This manifest type handles the visual representation of tool calls:
 * - Custom UI elements for tool-specific rendering (Generative UI)
 * - Icon and label for default status indicators
 *
 * Rendering concerns only -- does NOT handle tool execution or HITL approval.
 * For browser-executable tools (including approval config), see ManifestUaiAgentFrontendTool.
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
 * // Default-kind renderer (status indicator only -- no custom UI)
 * const renderer: ManifestUaiAgentToolRenderer = {
 *     type: "uaiAgentToolRenderer",
 *     kind: "default",
 *     alias: "My.AgentToolRenderer.SetProperty",
 *     meta: { toolName: "set_property_value", label: "Set Property Value", icon: "icon-edit" },
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
    };
}

/**
 * Default kind for uaiAgentToolRenderer extension type.
 *
 * Provides the default tool-status element for tool renderers that don't
 * specify a custom element (Generative UI).
 */
export const UAI_AGENT_TOOL_RENDERER_DEFAULT_KIND_MANIFEST: UmbExtensionManifestKind = {
    type: "kind",
    alias: "Uai.Kind.AgentToolRenderer.Default",
    matchKind: "default",
    matchType: "uaiAgentToolRenderer",
    manifest: {
        type: "uaiAgentToolRenderer",
        kind: "default",
        element: () => import("../components/tool-status.element.js"),
    },
};


declare global {
    interface UmbExtensionManifestMap {
        uaiAgentToolRenderer: ManifestUaiAgentToolRenderer;
    }
}
