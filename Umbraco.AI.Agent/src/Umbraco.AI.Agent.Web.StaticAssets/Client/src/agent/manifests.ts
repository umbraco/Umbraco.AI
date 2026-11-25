import { withAreaPermission } from "@umbraco-ai/core";
import { agentCollectionManifests } from "./collection/manifests.js";
import { agentEntityActionManifests } from "./entity-actions/manifests.js";
import { agentMenuManifests } from "./menu/manifests.js";
import { agentModalManifests } from "./modals/manifests.js";
import { UAI_PERMISSION_VERB_MANAGE_AGENTS } from "./permissions/constants.js";
import { agentPermissionManifests } from "./permissions/manifests.js";
import { agentPropertyActionManifests } from "./property-actions/manifests.js";
import { agentRepositoryManifests } from "./repository/manifests.js";
import { agentWorkspaceManifests } from "./workspace/manifests.js";

// Note: Tools and approval manifests have been moved to Umbraco.AI.Agent.Copilot package

export const agentManifests = [
    ...withAreaPermission(UAI_PERMISSION_VERB_MANAGE_AGENTS, [
        ...agentCollectionManifests,
        ...agentEntityActionManifests,
        ...agentMenuManifests,
        ...agentWorkspaceManifests,
    ]),
    ...agentModalManifests,
    ...agentPermissionManifests,
    ...agentPropertyActionManifests,
    ...agentRepositoryManifests,
];
