export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Umbraco AI Prompt Entrypoint",
    alias: "Umbraco.Ai.Agent.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
