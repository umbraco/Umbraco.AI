import { manifests as testsRootManifests } from "./tests-root/manifests.js";
import { manifests as testManifests } from "./test/manifests.js";

export const testsWorkspaceManifests: Array<UmbExtensionManifest> = [...testsRootManifests, ...testManifests];
