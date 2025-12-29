import type { ManifestGlobalContext } from "@umbraco-cms/backoffice/extension-registry";
import { headerAppManifests } from "./components/header-app/manifests.js";
import { sidebarManifests } from "./components/sidebar/manifests.js";

const globalContextManifest: ManifestGlobalContext = {
  type: "globalContext",
  alias: "UmbracoAiAgent.Copilot.GlobalContext",
  name: "Umbraco AI Agent Copilot Global Context",
  api: () => import("./copilot.context.js"),
};

export const copilotManifests = [
  ...headerAppManifests,
  ...sidebarManifests,
  globalContextManifest,
];
