import { analyticsMenuManifests } from './menu/manifests.js';
import { analyticsWorkspaceManifests } from './workspace/manifests.js';

export const analyticsManifests: Array<UmbExtensionManifest> = [
    ...analyticsMenuManifests,
    ...analyticsWorkspaceManifests,
];
