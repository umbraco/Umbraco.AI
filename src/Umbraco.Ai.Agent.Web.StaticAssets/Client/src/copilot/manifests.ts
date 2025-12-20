import { headerAppManifests } from "./header-app/manifests.js";
import { sidebarManifests } from "./sidebar/manifests.js";

export const copilotManifests = [
  ...headerAppManifests,
  ...sidebarManifests,
  {
    type: "globalContext",
    alias: "UmbracoAiAgent.Copilot.GlobalContext",
    name: "Umbraco AI Agent Copilot Global Context",
    api: () => import("./copilot.context.js")
  }
];
