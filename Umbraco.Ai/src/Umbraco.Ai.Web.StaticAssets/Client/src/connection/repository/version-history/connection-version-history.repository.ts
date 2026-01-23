import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import {
    UaiVersionHistoryResponse,
    UaiVersionComparisonResponse,
    UaiVersionHistoryTypeMapper,
} from "../../../core/version-history/exports.js";
import { ConnectionsService } from "../../../api";

/**
 * Repository for Connection version history operations.
 * Handles fetching version history, comparing versions, and rolling back.
 */
export class UaiConnectionVersionHistoryRepository {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets the version history for a connection.
     * @param connectionId - The connection ID.
     * @param skip - Number of versions to skip (for pagination).
     * @param take - Number of versions to return.
     * @returns The version history response.
     */
    async getVersionHistory(
        connectionId: string,
        skip: number,
        take: number
    ): Promise<UaiVersionHistoryResponse | undefined> {
        const { data, error } = await tryExecute(
            this.#host,
            ConnectionsService.getConnectionVersionHistory({
                path: {
                    connectionIdOrAlias: connectionId,
                },
                query: {
                    skip,
                    take,
                },
            })
        );

        if (error || !data) {
            console.error("Failed to load connection version history:", error);
            return undefined;
        }

        return UaiVersionHistoryTypeMapper.mapToVersionHistoryResponse(data);
    }

    /**
     * Compares two versions of a connection.
     * @param connectionId - The connection ID.
     * @param fromVersion - The source version number.
     * @param toVersion - The target version number.
     * @returns The comparison response with property changes.
     */
    async compareVersions(
        connectionId: string,
        fromVersion: number,
        toVersion: number
    ): Promise<UaiVersionComparisonResponse | undefined> {
        const { data, error } = await tryExecute(
            this.#host,
            ConnectionsService.compareConnectionVersions({
                path: {
                    connectionIdOrAlias: connectionId,
                    snapshotFromVersion: fromVersion,
                    snapshotToVersion: toVersion,
                },
            })
        );

        if (error || !data) {
            console.error("Failed to compare connection versions:", error);
            return undefined;
        }

        return UaiVersionHistoryTypeMapper.mapToComparisonResponse(data);
    }

    /**
     * Rolls back a connection to a previous version.
     * @param connectionId - The connection ID.
     * @param version - The version number to rollback to.
     * @returns True if rollback was successful.
     */
    async rollback(connectionId: string, version: number): Promise<boolean> {
        const { error } = await tryExecute(
            this.#host,
            ConnectionsService.rollbackConnectionToVersion({
                path: {
                    connectionIdOrAlias: connectionId,
                    snapshotVersion: version,
                },
            })
        );

        if (error) {
            console.error("Failed to rollback connection:", error);
            return false;
        }

        return true;
    }
}
