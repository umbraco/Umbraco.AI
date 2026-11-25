import { connectionCollectionManifests } from "./collection/manifests.js";
import { connectionEntityActionManifests } from "./entity-actions/manifests.js";
import { connectionMenuManifests } from "./menu/manifests.js";
import { connectionModalManifests } from "./modals/manifests.js";
import { connectionRepositoryManifests } from "./repository/manifests.js";
import { connectionWorkspaceManifests } from "./workspace/manifests.js";
import { withAreaPermission } from "../permissions/area-conditions.js";
import { UAI_PERMISSION_VERB_MANAGE_CONNECTIONS } from "../permissions/constants.js";

export const connectionManifests = [
    ...withAreaPermission(UAI_PERMISSION_VERB_MANAGE_CONNECTIONS, [
        ...connectionCollectionManifests,
        ...connectionEntityActionManifests,
        ...connectionMenuManifests,
        ...connectionWorkspaceManifests,
    ]),
    ...connectionModalManifests,
    ...connectionRepositoryManifests,
];
