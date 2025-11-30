import { promptCollectionManifests } from "./collection/manifests.js";
import { promptEntityActionManifests } from "./entity-actions/manifests.js";
import { promptMenuManifests } from "./menu/manifests.js";
import { promptRepositoryManifests } from "./repository/manifests.js";
import { promptWorkspaceManifests } from "./workspace/manifests.js";

export const promptManifests = [
    ...promptCollectionManifests,
    ...promptEntityActionManifests,
    ...promptMenuManifests,
    ...promptRepositoryManifests,
    ...promptWorkspaceManifests,
];
