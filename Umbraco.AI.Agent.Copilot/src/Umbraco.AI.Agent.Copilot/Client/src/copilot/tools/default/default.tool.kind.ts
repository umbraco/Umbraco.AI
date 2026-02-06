import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";

/**
 * Default kind for uaiAgentTool extension type.
 *
 * Provides the default tool-status element for tools that don't
 * specify a custom element (Generative UI).
 */
export const UAI_AGENT_TOOL_DEFAULT_KIND_MANIFEST: UmbExtensionManifestKind = {
    type: "kind",
    alias: "Uai.Kind.AgentTool.Default",
    matchKind: "default",
    matchType: "uaiAgentTool",
    manifest: {
        type: "uaiAgentTool",
        kind: "default",
        element: () => import("../tool-status.element.js"),
    },
};
