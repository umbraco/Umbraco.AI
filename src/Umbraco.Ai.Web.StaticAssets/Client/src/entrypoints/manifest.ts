export const manifests: Array<UmbExtensionManifest> = [
  {
    name: "Umbraco AI Entrypoint",
    alias: "Umbraco.Ai.Entrypoint",
    type: "backofficeEntryPoint",
    js: () => import("./entrypoint.js"),
  },
];
