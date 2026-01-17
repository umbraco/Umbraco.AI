import { manifests as promptManifests } from './prompt/manifests.js';
import { manifests as promptRootManifests } from './prompt-root/manifests.js';

export const promptWorkspaceManifests: Array<UmbExtensionManifest> = [
    ...promptManifests,
    ...promptRootManifests
];
