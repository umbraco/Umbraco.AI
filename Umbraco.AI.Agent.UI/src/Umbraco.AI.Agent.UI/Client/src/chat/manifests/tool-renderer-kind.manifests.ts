import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";

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
