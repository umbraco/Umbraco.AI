import type { ManifestGlobalContext } from "@umbraco-cms/backoffice/extension-registry";
import { componentManifests } from "./components/manifests.js";

const globalContextManifest: ManifestGlobalContext = {
  type: "globalContext",
  alias: "UmbracoAiAgent.Copilot.GlobalContext",
  name: "Umbraco AI Agent Copilot Global Context",
  api: () => import("./copilot.context.js"),
};

export const copilotManifests = [
  ...componentManifests,
  globalContextManifest,
];
