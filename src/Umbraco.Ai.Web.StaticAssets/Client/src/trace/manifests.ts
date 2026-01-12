import { traceCollectionManifests } from "./collection/manifests.js";
import { traceRepositoryManifests } from "./repository/manifests.js";
import { traceMenuManifests } from "./menu/manifests.js";

/**
 * Manifests for the Trace feature.
 *
 * Phase 5 includes:
 * - Repository manifests (collection and detail)
 * - Collection manifests (collection and table view)
 *
 * Phase 8.1 includes:
 * - Menu manifests (AI Traces menu item)
 *
 * Future phases will add:
 * - Workspace manifests (detail view)
 * - Workspace panel manifests (AI History panel for content items)
 */
export const traceManifests = [
    ...traceRepositoryManifests,
    ...traceCollectionManifests,
    ...traceMenuManifests,
];
