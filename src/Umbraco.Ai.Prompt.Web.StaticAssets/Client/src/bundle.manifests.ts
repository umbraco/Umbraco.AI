import { manifests as entrypoints } from "./entrypoints/manifest.js";
import { promptManifests } from "./prompt/manifests.js";

// Aggregate all manifests into a single bundle
export const manifests: Array<UmbExtensionManifest> = [
    ...entrypoints,
    ...promptManifests,
];
