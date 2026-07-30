// The CMS augments UmbExtensionManifestMap with the workspaceContext manifest under this key.
type ManifestWorkspaceContext = UmbExtensionManifestMap["ManifestWorkspaceContext"];

/**
 * Registered per-workspace; self-gates to supported workspaces and mounts the floating copilot button.
 */
const fabInjectorManifest: ManifestWorkspaceContext = {
    type: "workspaceContext",
    alias: "UmbracoAIAgent.WorkspaceContext.CopilotFab",
    name: "Copilot Floating Button Injector",
    api: () => import("./copilot-fab.context.js"),
};

export const copilotFabManifests = [fabInjectorManifest];
