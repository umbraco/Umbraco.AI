/**
 * Bundle Entry Point
 *
 * This file is the entry point for Umbraco's bundle loader.
 * It ONLY exports the manifests array - nothing else.
 *
 * IMPORTANT: Umbraco's bundle loader iterates over ALL exports looking for
 * manifest-like structures (objects with a `type` property). Exporting anything
 * else (class instances, context tokens, singletons) will cause errors.
 *
 * For the public API (@umbraco-ai/core), see umbraco-ai.ts which exports
 * the full public API for consumers.
 */

import { sectionManifests } from "./section/manifests.js";
import { connectionManifests } from "./connection/manifests.js";
import { contextManifests } from "./context/manifests.js";
import { contextResourceTypeManifests } from "./context-resource-type/manifests.js";
import { entityAdapterManifests } from "./entity-adapter/manifests.js";
import { profileManifests } from "./profile/manifests.js";
import { providerManifests } from "./provider/manifests.js";
import { propertyEditorManifests } from "./property-editors/manifests.js";
import { traceManifests } from "./trace/manifests.js";
import { manifests as langManifests } from "./lang/manifests.js";
import { manifests as coreManifests } from "./core/manifests.js";
import { workspaceRegistryManifests } from "./workspace-registry/manifests.js";

// Aggregate all manifests into a single bundle
// IMPORTANT: This should only include manifest arrays and nothing else
export const manifests: Array<UmbExtensionManifest> = [
  ...sectionManifests,
  ...connectionManifests,
  ...contextResourceTypeManifests,
  ...contextManifests,
  ...entityAdapterManifests,
  ...profileManifests,
  ...providerManifests,
  ...propertyEditorManifests,
  ...traceManifests,
  ...langManifests,
  ...coreManifests,
  ...workspaceRegistryManifests,
];
