import type { ManifestGlobalContext } from "@umbraco-cms/backoffice/extension-registry";
import type { ManifestUaiRequestContextContributor } from "@umbraco-ai/core";
import { componentManifests } from "./components/manifests.js";
import { manifests as toolManifests } from "./tools/manifests.js";
import type { ManifestUaiCopilotCompatibleSection } from "./types.js";

const globalContextManifest: ManifestGlobalContext = {
    type: "globalContext",
    alias: "UmbracoAIAgent.Copilot.GlobalContext",
    name: "Umbraco AI Agent Copilot Global Context",
    api: () => import("./copilot.context.js"),
};

/**
 * Core section compatibility declarations.
 *
 * These declare that the content and media sections support copilot.
 * Third-party packages can add their own section compatibility via manifests.
 */
export const copilotSectionManifests: Array<ManifestUaiCopilotCompatibleSection> = [
    {
        type: "uaiCopilotCompatibleSection",
        alias: "UmbracoAI.Copilot.Section.Content",
        name: "Content Section Copilot Support",
        section: "Umb.Section.Content",
    },
    {
        type: "uaiCopilotCompatibleSection",
        alias: "UmbracoAI.Copilot.Section.Media",
        name: "Media Section Copilot Support",
        section: "Umb.Section.Media",
    },
];

/**
 * Copilot-specific request context contributors.
 * Uses the "surface" kind from Agent.UI — only meta.surface is needed.
 */
export const copilotRequestContextManifests: ManifestUaiRequestContextContributor[] = [
    {
        type: "uaiRequestContextContributor",
        kind: "surface",
        alias: "UmbracoAI.Copilot.RequestContextContributor.Surface",
        name: "Copilot Surface Request Context Contributor",
        meta: { surface: "copilot" },
        weight: 50,
    },
];

export const copilotManifests = [
    ...componentManifests,
    ...toolManifests,
    ...copilotSectionManifests,
    ...copilotRequestContextManifests,
    globalContextManifest,
];
