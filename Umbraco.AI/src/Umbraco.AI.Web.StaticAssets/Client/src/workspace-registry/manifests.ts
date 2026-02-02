import type { ManifestGlobalContext } from "@umbraco-cms/backoffice/extension-registry";

const globalContextManifest: ManifestGlobalContext = {
	type: "globalContext",
	alias: "UmbracoAI.WorkspaceRegistry.GlobalContext",
	name: "Umbraco AI Workspace Registry Global Context",
	api: () => import("./workspace-registry.context.js"),
};

export const workspaceRegistryManifests = [globalContextManifest];
