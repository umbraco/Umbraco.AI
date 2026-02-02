import { manifests as agentManifests } from './agent/manifests.js';
import { manifests as agentRootManifests } from './agent-root/manifests.js';

export const agentWorkspaceManifests: Array<UmbExtensionManifest> = [
    ...agentManifests,
    ...agentRootManifests
];
