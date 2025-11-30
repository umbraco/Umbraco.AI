import { promptManifests } from "./prompt/manifests.js";

// Aggregate all manifests into a single bundle
export const manifests: Array<UmbExtensionManifest> = [
    ...promptManifests,
];
