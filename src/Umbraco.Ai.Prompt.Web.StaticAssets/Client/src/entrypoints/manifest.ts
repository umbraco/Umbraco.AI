export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Umbraco AI Prompt Entrypoint",
    alias: "Umbraco.Ai.Prompt.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
