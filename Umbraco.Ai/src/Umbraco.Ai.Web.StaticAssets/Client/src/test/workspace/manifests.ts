import type { UmbExtensionManifest } from "@umbraco-cms/backoffice/extension-registry";
import { manifests as testManifests } from './test/manifests.js';
import { manifests as testRootManifests } from './test-root/manifests.js';

export const testWorkspaceManifests: Array<UmbExtensionManifest> = [
    ...testManifests,
    ...testRootManifests
];
