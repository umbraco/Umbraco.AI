import { profileCollectionManifests } from "./collection/manifests.js";
import { profileEntityActionManifests } from "./entity-actions/manifests.js";
import { profileMenuManifests } from "./menu/manifests.js";
import { profileModalManifests } from "./modals/manifests.js";
import { profileRepositoryManifests } from "./repository/manifests.js";
import { profileWorkspaceManifests } from "./workspace/manifests.js";

export const profileManifests = [
    ...profileCollectionManifests,
    ...profileEntityActionManifests,
    ...profileMenuManifests,
    ...profileModalManifests,
    ...profileRepositoryManifests,
    ...profileWorkspaceManifests,
];
