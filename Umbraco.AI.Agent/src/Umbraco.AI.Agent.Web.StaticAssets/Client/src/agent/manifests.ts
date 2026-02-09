import { agentCollectionManifests } from "./collection/manifests.js";
import { agentEntityActionManifests } from "./entity-actions/manifests.js";
import { agentMenuManifests } from "./menu/manifests.js";
import { agentModalManifests } from "./modals/manifests.js";
import { agentRepositoryManifests } from "./repository/manifests.js";
import { agentWorkspaceManifests } from "./workspace/manifests.js";

// Note: Tools and approval manifests have been moved to Umbraco.AI.Agent.Copilot package

export const agentManifests = [
    ...agentCollectionManifests,
    ...agentEntityActionManifests,
    ...agentMenuManifests,
    ...agentModalManifests,
    ...agentRepositoryManifests,
    ...agentWorkspaceManifests,
];
