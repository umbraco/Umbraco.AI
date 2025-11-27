import { manifests as entrypoints } from "./entrypoints/manifest.js";
import { sectionManifests } from "./section/manifests.js";
import { connectionManifests } from "./connection/manifests.js";
import { providerManifests } from "./provider/manifests.js";

// Job of the bundle is to collate all the manifests from different parts of the extension and load other manifests
// We load this bundle from umbraco-package.json
export const manifests: Array<UmbExtensionManifest> = [
  ...entrypoints,
  ...sectionManifests,
  ...connectionManifests,
  ...providerManifests,
];
