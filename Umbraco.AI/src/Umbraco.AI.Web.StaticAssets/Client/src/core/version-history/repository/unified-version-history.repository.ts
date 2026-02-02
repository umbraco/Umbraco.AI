import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import {
    UaiVersionHistoryResponse,
    UaiVersionComparisonResponse,
    UaiVersionHistoryTypeMapper,
} from "../exports.js";
import { VersionsService } from "../../../api/index.js";

/**
 * Unified repository for version history operations across all entity types.
 * Uses the unified versioning API endpoint instead of entity-specific endpoints.
 */
export class UaiUnifiedVersionHistoryRepository {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets the version history for an entity.
     * @param entityType - The type of entity (profile, connection, context).
     * @param entityId - The entity ID.
     * @param skip - Number of versions to skip (for pagination).
     * @param take - Number of versions to return.
     * @returns The version history response.
     */
    async getVersionHistory(
        entityType: string,
        entityId: string,
        skip: number,
        take: number
    ): Promise<UaiVersionHistoryResponse | undefined> {
        const { data, error } = await tryExecute(
            this.#host,
            VersionsService.getVersionHistory({
                path: {
                    entityType,
                    entityId,
                },
                query: {
                    skip,
                    take,
                },
            })
        );

        if (error || !data) {
            console.error(`Failed to load ${entityType} version history:`, error);
            return undefined;
        }

        return UaiVersionHistoryTypeMapper.mapToVersionHistoryResponse(data);
    }

    /**
     * Compares two versions of an entity.
     * @param entityType - The type of entity (profile, connection, context).
     * @param entityId - The entity ID.
     * @param fromVersion - The source version number.
     * @param toVersion - The target version number.
     * @returns The comparison response with property changes.
     */
    async compareVersions(
        entityType: string,
        entityId: string,
        fromVersion: number,
        toVersion: number
    ): Promise<UaiVersionComparisonResponse | undefined> {
        const { data, error } = await tryExecute(
            this.#host,
            VersionsService.compareVersions({
                path: {
                    entityType,
                    entityId,
                    fromEntityVersion: fromVersion,
                    toEntityVersion: toVersion,
                },
            })
        );

        if (error || !data) {
            console.error(`Failed to compare ${entityType} versions:`, error);
            return undefined;
        }

        return UaiVersionHistoryTypeMapper.mapToComparisonResponse(data);
    }

    /**
     * Rolls back an entity to a previous version.
     * @param entityType - The type of entity (profile, connection, context).
     * @param entityId - The entity ID.
     * @param version - The version number to rollback to.
     * @returns True if rollback was successful.
     */
    async rollback(
        entityType: string,
        entityId: string,
        version: number
    ): Promise<boolean> {
        const { error } = await tryExecute(
            this.#host,
            VersionsService.rollbackToVersion({
                path: {
                    entityType,
                    entityId,
                    entityVersion: version,
                },
            })
        );

        if (error) {
            console.error(`Failed to rollback ${entityType}:`, error);
            return false;
        }

        return true;
    }
}
