import { knowledgeSetCollectionManifests } from "./collection/manifests.js";
import { knowledgeSetComponentManifests } from "./components/manifests.js";
import { knowledgeSetMenuManifests } from "./menu/manifests.js";
import { knowledgeSetRepositoryManifests } from "./repository/manifests.js";
import { knowledgeSetWorkspaceManifests } from "./workspace/manifests.js";
import { withAreaPermission } from "../permissions/area-conditions.js";
import { UAI_PERMISSION_VERB_MANAGE_KNOWLEDGE_SETS } from "../permissions/constants.js";

/**
 * Manifests for the Knowledge Set feature.
 *
 * Read-only, unlike Context: a listing (collection + table view), a read-only per-set audit workspace
 * (Details + Info views), the read-only item content modal, and a menu item. No create/edit/delete
 * entity actions, no bulk actions, no pickers.
 */
export const knowledgeSetManifests: Array<UmbExtensionManifest> = [
    ...withAreaPermission(UAI_PERMISSION_VERB_MANAGE_KNOWLEDGE_SETS, [
        ...knowledgeSetCollectionManifests,
        ...knowledgeSetMenuManifests,
        ...knowledgeSetWorkspaceManifests,
    ]),
    ...knowledgeSetComponentManifests,
    ...knowledgeSetRepositoryManifests,
];
