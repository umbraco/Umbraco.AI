import { manifests as testRootManifests } from "./test-root/manifests.js";
import { manifests as testManifests } from "./test/manifests.js";

export const testWorkspaceManifests: Array<UmbExtensionManifest> = [...testRootManifests, ...testManifests];
