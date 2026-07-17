import { knowledgeSetCollectionManifests } from "./collection/manifests.js";
import { knowledgeSetMenuManifests } from "./menu/manifests.js";
import { knowledgeSetRepositoryManifests } from "./repository/manifests.js";
import { knowledgeSetWorkspaceManifests } from "./workspace/manifests.js";

/**
 * Manifests for the Knowledge Set feature.
 *
 * Read-only, unlike Context: a listing (collection + table view) and menu item only. No create/edit/
 * delete entity actions. A read-only per-set audit workspace is added in a later phase.
 */
export const knowledgeSetManifests: Array<UmbExtensionManifest> = [
    ...knowledgeSetCollectionManifests,
    ...knowledgeSetMenuManifests,
    ...knowledgeSetRepositoryManifests,
    ...knowledgeSetWorkspaceManifests,
];
