import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";
import { manifests as langManifests } from "./lang/manifests.js";
import { coreManifests } from "./core/manifests.js";
import { agentManifests } from "./agent/manifests.js";
import { copilotManifests } from "./copilot/manifests.js";

// Aggregate all manifests into a single bundle
// Includes both regular manifests and kind manifests
export const manifests: Array<UmbExtensionManifest | UmbExtensionManifestKind> = [
    ...langManifests,
    ...coreManifests,
    ...agentManifests,
    ...copilotManifests,
];
