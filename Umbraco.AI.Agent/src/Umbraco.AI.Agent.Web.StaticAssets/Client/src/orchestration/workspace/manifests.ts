import { manifests as orchestrationManifests } from "./orchestration/manifests.js";
import { manifests as orchestrationRootManifests } from "./orchestration-root/manifests.js";

export const orchestrationWorkspaceManifests: Array<UmbExtensionManifest> = [
    ...orchestrationManifests,
    ...orchestrationRootManifests,
];
