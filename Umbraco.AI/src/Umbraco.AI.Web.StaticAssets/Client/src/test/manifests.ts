import { testMenuManifests } from "./menu/manifests.js";
import { testWorkspaceManifests } from "./workspace/manifests.js";
import { testEntityActionManifests } from "./entity-actions/manifests.js";
import { testRepositoryManifests } from "./repository/manifests.js";
import { testCollectionManifests } from "./collection/manifests.js";

export const testManifests: Array<UmbExtensionManifest> = [
	...testMenuManifests,
	...testRepositoryManifests,
	...testCollectionManifests,
	...testWorkspaceManifests,
	...testEntityActionManifests,
];
