import { profileCollectionManifests } from "./collection/manifests.js";
import { profileEntityActionManifests } from "./entity-actions/manifests.js";
import { profileMenuManifests } from "./menu/manifests.js";
import { profileModalManifests } from "./modals/manifests.js";
import { profileRepositoryManifests } from "./repository/manifests.js";
import { profileWorkspaceManifests } from "./workspace/manifests.js";
import { withAreaPermission } from "../permissions/area-conditions.js";
import { UAI_PERMISSION_VERB_MANAGE_PROFILES } from "../permissions/constants.js";

export const profileManifests = [
    ...withAreaPermission(UAI_PERMISSION_VERB_MANAGE_PROFILES, [
        ...profileCollectionManifests,
        ...profileEntityActionManifests,
        ...profileMenuManifests,
        ...profileWorkspaceManifests,
    ]),
    ...profileModalManifests,
    ...profileRepositoryManifests,
];
