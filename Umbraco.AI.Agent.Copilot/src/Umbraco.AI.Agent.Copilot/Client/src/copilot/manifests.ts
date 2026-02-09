import type { ManifestGlobalContext } from "@umbraco-cms/backoffice/extension-registry";
import { componentManifests } from "./components/manifests.js";
import { manifests as toolManifests } from "./tools/manifests.js";

const globalContextManifest: ManifestGlobalContext = {
    type: "globalContext",
    alias: "UmbracoAIAgent.Copilot.GlobalContext",
    name: "Umbraco AI Agent Copilot Global Context",
    api: () => import("./copilot.context.js"),
};

export const copilotManifests = [...componentManifests, ...toolManifests, globalContextManifest];
