import { settingsMenuManifests } from './menu/manifests.js';
import { settingsWorkspaceManifests } from './workspace/manifests.js';

export const settingsManifests: Array<UmbExtensionManifest> = [
    ...settingsMenuManifests,
    ...settingsWorkspaceManifests,
];
