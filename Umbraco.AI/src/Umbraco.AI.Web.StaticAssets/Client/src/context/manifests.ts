import { contextCollectionManifests } from "./collection/manifests.js";
import { resourceListModalManifests } from "./components/resource-list/manifests.js";
import { contextEntityActionManifests } from "./entity-actions/manifests.js";
import { contextMenuManifests } from "./menu/manifests.js";
import { contextRepositoryManifests } from "./repository/manifests.js";
import { contextWorkspaceManifests } from "./workspace/manifests.js";
import { withAreaPermission } from "../permissions/area-conditions.js";
import { UAI_PERMISSION_VERB_MANAGE_CONTEXTS } from "../permissions/constants.js";

export const contextManifests = [
    ...withAreaPermission(UAI_PERMISSION_VERB_MANAGE_CONTEXTS, [
        ...contextCollectionManifests,
        ...contextEntityActionManifests,
        ...contextMenuManifests,
        ...contextWorkspaceManifests,
    ]),
    ...resourceListModalManifests,
    ...contextRepositoryManifests,
];
