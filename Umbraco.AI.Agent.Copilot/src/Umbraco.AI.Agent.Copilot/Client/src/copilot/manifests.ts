import type { ManifestGlobalContext } from "@umbraco-cms/backoffice/extension-registry";
import { componentManifests } from "./components/manifests.js";
import { manifests as toolManifests } from "./tools/manifests.js";
import { manifests as approvalManifests } from "./approval/manifests.js";

const globalContextManifest: ManifestGlobalContext = {
  type: "globalContext",
  alias: "UmbracoAiAgent.Copilot.GlobalContext",
  name: "Umbraco AI Agent Copilot Global Context",
  api: () => import("./copilot.context.js"),
};

export const copilotManifests = [
  ...componentManifests,
  ...toolManifests,
  ...approvalManifests,
  globalContextManifest,
];
