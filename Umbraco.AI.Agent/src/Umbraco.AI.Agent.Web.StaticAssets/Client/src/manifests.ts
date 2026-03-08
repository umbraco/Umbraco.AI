import { manifests as langManifests } from "./lang/manifests.js";
import { coreManifests } from "./core/manifests.js";
import { agentManifests } from "./agent/manifests.js";

// Aggregate all manifests into a single bundle
// Note: Copilot manifests are now in Umbraco.AI.Agent.Copilot package
// Note: Orchestration manifests are now included in agentManifests (unified)
export const manifests: UmbExtensionManifest[] = [
    ...langManifests,
    ...coreManifests,
    ...agentManifests,
];
