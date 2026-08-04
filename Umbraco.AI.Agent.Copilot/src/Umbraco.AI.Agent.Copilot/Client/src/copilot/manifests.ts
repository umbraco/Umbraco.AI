import type { ManifestGlobalContext } from "@umbraco-cms/backoffice/extension-registry";
import type { ManifestUaiRequestContextContributor } from "@umbraco-ai/core";
import { componentManifests } from "./components/manifests.js";
import { manifests as toolManifests } from "./tools/manifests.js";

const globalContextManifest: ManifestGlobalContext = {
    type: "globalContext",
    alias: "UmbracoAIAgent.Copilot.GlobalContext",
    name: "Umbraco AI Agent Copilot Global Context",
    api: () => import("./copilot.context.js"),
};

/**
 * Copilot-specific request context contributors.
 * Uses the "agentSurface" kind from Agent.UI — only meta.surface is needed.
 */
export const copilotRequestContextManifests: ManifestUaiRequestContextContributor[] = [
    {
        type: "uaiRequestContextContributor",
        kind: "agentSurface",
        alias: "UmbracoAI.Copilot.RequestContextContributor.AgentSurface",
        name: "Copilot Agent Surface Request Context Contributor",
        meta: { surface: "copilot" },
        weight: 50,
    },
];

export const copilotManifests = [
    ...componentManifests,
    ...toolManifests,
    ...copilotRequestContextManifests,
    globalContextManifest,
];
