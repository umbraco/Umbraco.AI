import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";
import { copilotManifests } from "./copilot/manifests.js";
import { manifests as langManifests } from "./lang/manifests.js";

// Aggregate all manifests into a single bundle
// Includes copilot, tools, approval, and localization manifests
export const manifests: Array<UmbExtensionManifest | UmbExtensionManifestKind> = [
    ...langManifests,
    ...copilotManifests,
];
