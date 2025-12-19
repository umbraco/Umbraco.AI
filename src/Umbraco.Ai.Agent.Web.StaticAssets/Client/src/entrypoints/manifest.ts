export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Umbraco AI Agent Entrypoint",
    alias: "Umbraco.Ai.Agent.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
