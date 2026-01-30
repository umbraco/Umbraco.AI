import { manifests as connectionManifests } from './connection/manifests.js';
import { manifests as connectionRootManifests } from './connection-root/manifests.js';

export const connectionWorkspaceManifests: Array<UmbExtensionManifest> = [
    ...connectionManifests,
    ...connectionRootManifests
];
