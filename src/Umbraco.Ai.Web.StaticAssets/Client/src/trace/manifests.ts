import { traceCollectionManifests } from "./collection/manifests.js";
import { traceRepositoryManifests } from "./repository/manifests.js";

/**
 * Manifests for the Trace feature.
 *
 * Phase 5 includes:
 * - Repository manifests (collection and detail)
 * - Collection manifests (collection and table view)
 *
 * Future phases will add:
 * - Workspace manifests (detail view)
 * - Menu manifests (AI Traces menu item)
 * - Workspace panel manifests (AI History panel for content items)
 */
export const traceManifests = [
    ...traceRepositoryManifests,
    ...traceCollectionManifests,
];
