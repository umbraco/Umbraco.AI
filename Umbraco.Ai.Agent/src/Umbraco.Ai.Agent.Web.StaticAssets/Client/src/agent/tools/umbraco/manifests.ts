import type { ManifestUaiAgentTool } from "../uai-agent-tool.extension.js";

export const searchUmbracoManifest: ManifestUaiAgentTool = {
	type: "uaiAgentTool",
	kind: "default",
	alias: "Uai.AgentTool.SearchUmbraco",
	name: "Search Umbraco Tool",
	element: () => import("./search-umbraco.element.js"),
	meta: {
		toolName: "search_umbraco",
		label: "Search Umbraco",
		description: "Search Umbraco content and media",
		icon: "icon-search",
		// No api = backend tool (execution happens server-side)
	},
};

export const manifests = [searchUmbracoManifest];
