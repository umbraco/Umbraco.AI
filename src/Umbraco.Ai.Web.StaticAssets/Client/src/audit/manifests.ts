import { auditCollectionManifests } from "./collection/manifests.js";
import { auditRepositoryManifests } from "./repository/manifests.js";
import { auditMenuManifests } from "./menu/manifests.js";

/**
 * Manifests for the Audit feature.
 *
 * Phase 5 includes:
 * - Repository manifests (collection and detail)
 * - Collection manifests (collection and table view)
 *
 * Phase 8.1 includes:
 * - Menu manifests (AI Audits menu item)
 *
 * Future phases will add:
 * - Workspace manifests (detail view)
 * - Workspace panel manifests (AI History panel for content items)
 */
export const auditManifests = [
    ...auditRepositoryManifests,
    ...auditCollectionManifests,
    ...auditMenuManifests,
];
