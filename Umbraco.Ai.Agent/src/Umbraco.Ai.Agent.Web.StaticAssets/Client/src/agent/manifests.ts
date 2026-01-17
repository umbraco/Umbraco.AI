import { agentCollectionManifests } from "./collection/manifests.js";
import { agentEntityActionManifests } from "./entity-actions/manifests.js";
import { agentMenuManifests } from "./menu/manifests.js";
import { agentRepositoryManifests } from "./repository/manifests.js";
import { agentWorkspaceManifests } from "./workspace/manifests.js";
import { manifests as agentToolsManifests } from "./tools/manifests.js";
import { manifests as agentApprovalManifests } from "./approval/manifests.js";

export const agentManifests = [
    ...agentCollectionManifests,
    ...agentEntityActionManifests,
    ...agentMenuManifests,
    ...agentRepositoryManifests,
    ...agentWorkspaceManifests,
    ...agentToolsManifests,
    ...agentApprovalManifests,
];
