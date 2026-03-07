import { orchestrationCollectionManifests } from "./collection/manifests.js";
import { orchestrationEntityActionManifests } from "./entity-actions/manifests.js";
import { orchestrationMenuManifests } from "./menu/manifests.js";
import { orchestrationRepositoryManifests } from "./repository/manifests.js";
import { orchestrationWorkspaceManifests } from "./workspace/manifests.js";

export const orchestrationManifests = [
    ...orchestrationCollectionManifests,
    ...orchestrationEntityActionManifests,
    ...orchestrationMenuManifests,
    ...orchestrationRepositoryManifests,
    ...orchestrationWorkspaceManifests,
];
