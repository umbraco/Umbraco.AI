import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import {
    UaiVersionHistoryResponse,
    UaiVersionComparisonResponse,
    UaiVersionHistoryTypeMapper,
} from "../../../core/version-history/exports.js";
import { ContextsService } from "../../../api";

/**
 * Repository for Context version history operations.
 * Handles fetching version history, comparing versions, and rolling back.
 */
export class UaiContextVersionHistoryRepository {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets the version history for a context.
     * @param contextId - The context ID.
     * @param skip - Number of versions to skip (for pagination).
     * @param take - Number of versions to return.
     * @returns The version history response.
     */
    async getVersionHistory(
        contextId: string,
        skip: number,
        take: number
    ): Promise<UaiVersionHistoryResponse | undefined> {
        const { data, error } = await tryExecute(
            this.#host,
            ContextsService.getContextVersionHistory({
                path: {
                    contextIdOrAlias: contextId,
                },
                query: {
                    skip,
                    take,
                },
            })
        );

        if (error || !data) {
            console.error("Failed to load context version history:", error);
            return undefined;
        }

        return UaiVersionHistoryTypeMapper.mapToVersionHistoryResponse(data);
    }

    /**
     * Compares two versions of a context.
     * @param contextId - The context ID.
     * @param fromVersion - The source version number.
     * @param toVersion - The target version number.
     * @returns The comparison response with property changes.
     */
    async compareVersions(
        contextId: string,
        fromVersion: number,
        toVersion: number
    ): Promise<UaiVersionComparisonResponse | undefined> {
        const { data, error } = await tryExecute(
            this.#host,
            ContextsService.compareContextVersions({
                path: {
                    contextIdOrAlias: contextId,
                    snapshotFromVersion: fromVersion,
                    snapshotToVersion: toVersion,
                },
            })
        );

        if (error || !data) {
            console.error("Failed to compare context versions:", error);
            return undefined;
        }

        return UaiVersionHistoryTypeMapper.mapToComparisonResponse(data);
    }

    /**
     * Rolls back a context to a previous version.
     * @param contextId - The context ID.
     * @param version - The version number to rollback to.
     * @returns True if rollback was successful.
     */
    async rollback(contextId: string, version: number): Promise<boolean> {
        const { error } = await tryExecute(
            this.#host,
            ContextsService.rollbackContextToVersion({
                path: {
                    contextIdOrAlias: contextId,
                    snapshotVersion: version,
                },
            })
        );

        if (error) {
            console.error("Failed to rollback context:", error);
            return false;
        }

        return true;
    }
}
