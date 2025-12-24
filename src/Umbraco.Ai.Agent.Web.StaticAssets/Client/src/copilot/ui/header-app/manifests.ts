import type { ManifestHeaderApp } from "@umbraco-cms/backoffice/extension-registry";

export const headerAppManifests: ManifestHeaderApp[] = [
  {
    type: "headerApp",
    alias: "UmbracoAiAgent.HeaderApp.Copilot",
    name: "AI Copilot Header App",
    element: () => import("./copilot-header-app.element.js"),
    weight: 100,
  },
];
