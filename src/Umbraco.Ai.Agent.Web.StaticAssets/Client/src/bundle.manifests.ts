import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";
import { manifests as entrypoints } from "./entrypoints/manifest.js";
import { manifests as langManifests } from "./lang/manifests.js";
import { iconManifests } from "./core/icons/index.js";
import { agentManifests } from "./agent/manifests.js";
import { copilotManifests } from "./copilot/manifests.js";

// Aggregate all manifests into a single bundle
// Includes both regular manifests and kind manifests
export const manifests: Array<UmbExtensionManifest | UmbExtensionManifestKind> = [
    ...entrypoints,
    ...langManifests,
    ...iconManifests,
    ...agentManifests,
    ...copilotManifests,
];
