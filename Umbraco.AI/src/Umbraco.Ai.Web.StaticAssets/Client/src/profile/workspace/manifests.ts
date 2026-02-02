import { manifests as profileManifests } from './profile/manifests.js';
import { manifests as profileRootManifests } from './profile-root/manifests.js';

export const profileWorkspaceManifests: Array<UmbExtensionManifest> = [
    ...profileManifests,
    ...profileRootManifests
];
