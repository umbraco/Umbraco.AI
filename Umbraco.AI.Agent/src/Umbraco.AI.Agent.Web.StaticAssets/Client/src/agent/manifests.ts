import { agentCollectionManifests } from "./collection/manifests.js";
import { agentEntityActionManifests } from "./entity-actions/manifests.js";
import { agentMenuManifests } from "./menu/manifests.js";
import { agentRepositoryManifests } from "./repository/manifests.js";
import { agentWorkspaceManifests } from "./workspace/manifests.js";

// Note: Tools and approval manifests have been moved to Umbraco.Ai.Agent.Copilot package

export const agentManifests = [
    ...agentCollectionManifests,
    ...agentEntityActionManifests,
    ...agentMenuManifests,
    ...agentRepositoryManifests,
    ...agentWorkspaceManifests,
];
