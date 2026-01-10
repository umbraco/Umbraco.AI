import { manifests as entrypoints } from "./entrypoints/manifest.js";
import { sectionManifests } from "./section/manifests.js";
import { connectionManifests } from "./connection/manifests.js";
import { contextManifests } from "./context/manifests.js";
import { contextResourceTypeManifests } from "./context-resource-type/manifests.js";
import { entityAdapterManifests } from "./entity-adapter/adapters/manifests.js";
import { profileManifests } from "./profile/manifests.js";
import { providerManifests } from "./provider/manifests.js";
import { propertyEditorManifests } from "./property-editors/manifests.js";
import { manifests as langManifests } from "./lang/manifests.js";
import { manifests as coreManifests } from "./core/manifests.js";

// Re-export everything from index for easier imports
export * from './index.js';

// Public API exports for @umbraco-ai/core import map
export * from './exports.js';

// Aggregate all manifests into a single bundle
export const manifests: Array<UmbExtensionManifest> = [
  ...entrypoints,
  ...sectionManifests,
  ...connectionManifests,
  ...contextResourceTypeManifests,
  ...contextManifests,
  ...entityAdapterManifests,
  ...profileManifests,
  ...providerManifests,
  ...propertyEditorManifests,
  ...langManifests,
  ...coreManifests,
];
