import type { ManifestBackofficeEntryPoint } from "@umbraco-cms/backoffice/extension-registry";

export const sidebarManifests: ManifestBackofficeEntryPoint[] = [
    {
        type: "backofficeEntryPoint",
        alias: "UmbracoAIAgent.BackofficeEntryPoint.CopilotSidebar",
        name: "AI Copilot Sidebar Entry Point",
        js: () => import("./entry-point.js"),
    },
];
