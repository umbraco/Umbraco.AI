import { manifests as guardrailManifests } from "./guardrail/manifests.js";
import { manifests as guardrailRootManifests } from "./guardrail-root/manifests.js";

export const guardrailWorkspaceManifests: Array<UmbExtensionManifest> = [
    ...guardrailManifests,
    ...guardrailRootManifests,
];
