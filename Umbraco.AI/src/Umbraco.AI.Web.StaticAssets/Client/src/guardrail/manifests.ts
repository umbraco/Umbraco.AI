import { guardrailCollectionManifests } from "./collection/manifests.js";
import { guardrailEntityActionManifests } from "./entity-actions/manifests.js";
import { guardrailMenuManifests } from "./menu/manifests.js";
import { guardrailModalManifests } from "./modals/manifests.js";
import { guardrailRepositoryManifests } from "./repository/manifests.js";
import { guardrailWorkspaceManifests } from "./workspace/manifests.js";
import { withAreaPermission } from "../permissions/area-conditions.js";
import { UAI_PERMISSION_VERB_MANAGE_GUARDRAILS } from "../permissions/constants.js";

export const guardrailManifests = [
    ...withAreaPermission(UAI_PERMISSION_VERB_MANAGE_GUARDRAILS, [
        ...guardrailCollectionManifests,
        ...guardrailEntityActionManifests,
        ...guardrailMenuManifests,
        ...guardrailWorkspaceManifests,
    ]),
    ...guardrailModalManifests,
    ...guardrailRepositoryManifests,
];
