import { testMenuManifests } from "./menu/manifests.js";
import { testsWorkspaceManifests } from "./workspace/manifests.js";

export const testsManifests: Array<UmbExtensionManifest> = [...testMenuManifests, ...testsWorkspaceManifests];
