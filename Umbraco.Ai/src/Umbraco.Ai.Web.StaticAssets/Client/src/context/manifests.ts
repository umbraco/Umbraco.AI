import { contextCollectionManifests } from "./collection/manifests.js";
import { resourceListModalManifests } from "./components/resource-list/manifests.js";
import { contextEntityActionManifests } from "./entity-actions/manifests.js";
import { contextMenuManifests } from "./menu/manifests.js";
import { contextRepositoryManifests } from "./repository/manifests.js";
import { contextWorkspaceManifests } from "./workspace/manifests.js";

export const contextManifests = [
    ...contextCollectionManifests,
    ...resourceListModalManifests,
    ...contextEntityActionManifests,
    ...contextMenuManifests,
    ...contextRepositoryManifests,
    ...contextWorkspaceManifests,
];
