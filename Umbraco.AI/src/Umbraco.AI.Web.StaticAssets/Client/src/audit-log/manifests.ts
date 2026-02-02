import { auditLogCollectionManifests } from "./collection/manifests.js";
import { auditLogRepositoryManifests } from "./repository/manifests.js";
import { auditLogWorkspaceManifests } from "./workspace/manifests.js";
import { auditLogMenuManifests } from "./menu/manifests.js";

/**
 * Manifests for the AuditLog feature.
 *
 * Phase 5 includes:
 * - Repository manifests (collection and detail) 
 * - Collection manifests (collection and table view)
 *
 * Phase 8.1 includes:
 * - Menu manifests (AI AuditLog Logs menu item)
 *
 * Future phases will add:
 * - Workspace manifests (detail view)
 * - Workspace panel manifests (AI History panel for content items)
 */
export const auditLogManifests = [
    ...auditLogRepositoryManifests,
    ...auditLogCollectionManifests,
    ...auditLogWorkspaceManifests,
    ...auditLogMenuManifests,
];
