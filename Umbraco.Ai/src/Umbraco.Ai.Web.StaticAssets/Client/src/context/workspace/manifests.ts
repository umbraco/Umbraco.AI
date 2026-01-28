import { manifests as contextManifests } from './context/manifests.js';
import { manifests as contextRootManifests } from './context-root/manifests.js';

export const contextWorkspaceManifests: Array<UmbExtensionManifest> = [
    ...contextManifests,
    ...contextRootManifests
];
