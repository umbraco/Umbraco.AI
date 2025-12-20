import type { ManifestBackofficeEntryPoint } from "@umbraco-cms/backoffice/extension-registry";

export const sidebarManifests: ManifestBackofficeEntryPoint[] = [
  {
    type: "backofficeEntryPoint",
    alias: "UmbracoAiAgent.BackofficeEntryPoint.CopilotSidebar",
    name: "AI Copilot Sidebar Entry Point",
    js: () => import("./copilot-sidebar.element.js"),
  },
];
