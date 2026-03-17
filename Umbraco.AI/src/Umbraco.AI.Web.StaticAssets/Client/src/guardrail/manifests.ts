import { guardrailCollectionManifests } from "./collection/manifests.js";
import { guardrailEntityActionManifests } from "./entity-actions/manifests.js";
import { guardrailMenuManifests } from "./menu/manifests.js";
import { guardrailModalManifests } from "./modals/manifests.js";
import { guardrailRepositoryManifests } from "./repository/manifests.js";
import { guardrailWorkspaceManifests } from "./workspace/manifests.js";

export const guardrailManifests = [
    ...guardrailCollectionManifests,
    ...guardrailEntityActionManifests,
    ...guardrailMenuManifests,
    ...guardrailModalManifests,
    ...guardrailRepositoryManifests,
    ...guardrailWorkspaceManifests,
];
