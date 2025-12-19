import { agentCollectionManifests } from "./collection/manifests.js";
import { agentEntityActionManifests } from "./entity-actions/manifests.js";
import { agentMenuManifests } from "./menu/manifests.js";
import { agentRepositoryManifests } from "./repository/manifests.js";
import { agentWorkspaceManifests } from "./workspace/manifests.js";

export const agentManifests = [
    ...agentCollectionManifests,
    ...agentEntityActionManifests,
    ...agentMenuManifests,
    ...agentRepositoryManifests,
    ...agentWorkspaceManifests,
];
