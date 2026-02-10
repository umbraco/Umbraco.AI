import type { ManifestUaiAgentToolRenderer } from "@umbraco-ai/agent-ui";

export const searchUmbracoManifest: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    alias: "Uai.AgentToolRenderer.SearchUmbraco",
    name: "Search Umbraco Tool Renderer",
    element: () => import("./search-umbraco.element.js"),
    meta: {
        toolName: "search_umbraco",
        label: "Search Umbraco",
        icon: "icon-search",
    },
};

export const manifests = [searchUmbracoManifest];
