import type { Observable } from "@umbraco-cms/backoffice/external/rxjs";
import type {
    UaiVersionHistoryResponse,
    UaiVersionComparisonResponse,
} from "../types.js";

/**
 * Interface for workspace contexts that support version history.
 * Workspaces that manage versionable entities should implement this interface
 * to enable version history, comparison, and rollback functionality.
 */
export interface UaiVersionableEntityWorkspaceContext {
    /**
     * Observable for the current version number.
     * Returns undefined if the entity hasn't been loaded yet.
     */
    readonly version: Observable<number | undefined>;

    /**
     * Gets the version history for the entity.
     * @param skip Number of versions to skip (for pagination).
     * @param take Number of versions to return (for pagination).
     * @returns Promise resolving to the version history response.
     */
    getVersionHistory(skip: number, take: number): Promise<UaiVersionHistoryResponse | undefined>;

    /**
     * Compares two versions of the entity.
     * @param fromVersion The source version to compare from.
     * @param toVersion The target version to compare to.
     * @returns Promise resolving to the comparison response.
     */
    compareVersions(fromVersion: number, toVersion: number): Promise<UaiVersionComparisonResponse | undefined>;

    /**
     * Rolls back the entity to a previous version.
     * This creates a new version with the content from the target version.
     * @param version The version number to rollback to.
     * @returns Promise that resolves when rollback is complete.
     */
    rollbackToVersion(version: number): Promise<void>;
}
