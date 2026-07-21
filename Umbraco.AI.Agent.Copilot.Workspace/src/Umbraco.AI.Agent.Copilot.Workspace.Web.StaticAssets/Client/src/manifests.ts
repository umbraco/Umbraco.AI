import type { UmbExtensionManifestKind } from "@umbraco-cms/backoffice/extension-registry";
import { manifests as langManifests } from "./lang/manifests.js";
import { sectionManifests } from "./section/manifests.js";

// Aggregate all manifests into the bundle loaded by umbraco-package.json.
export const manifests: Array<UmbExtensionManifest | UmbExtensionManifestKind> = [
    ...langManifests,
    ...sectionManifests,
];
