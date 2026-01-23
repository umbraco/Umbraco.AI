import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { client } from "../../../api/client.gen.js";
import type {
    UaiVersionHistoryResponse,
    UaiVersionComparisonResponse,
    UaiVersionHistoryItem,
    UaiVersionPropertyChange,
} from "../../../core/version-history/exports.js";

/**
 * API response type for version history (matches backend EntityVersionHistoryResponseModel).
 */
interface VersionHistoryApiResponse {
    currentVersion: number;
    totalVersions: number;
    versions: Array<{
        id: string;
        entityId: string;
        version: number;
        dateCreated: string;
        createdByUserId?: number;
        createdByUserName?: string;
        changeDescription?: string;
    }>;
}

/**
 * API response type for version comparison (matches backend VersionComparisonResponseModel).
 */
interface VersionComparisonApiResponse {
    fromVersion: number;
    toVersion: number;
    changes: Array<{
        propertyName: string;
        oldValue?: string;
        newValue?: string;
    }>;
}

/**
 * Repository for Profile version history operations.
 * Handles fetching version history, comparing versions, and rolling back.
 */
export class UaiProfileVersionHistoryRepository {
    #host: UmbControllerHost;

    constructor(host: UmbControllerHost) {
        this.#host = host;
    }

    /**
     * Gets the version history for a profile.
     * @param profileId - The profile ID.
     * @param skip - Number of versions to skip (for pagination).
     * @param take - Number of versions to return.
     * @returns The version history response.
     */
    async getVersionHistory(
        profileId: string,
        skip: number,
        take: number
    ): Promise<UaiVersionHistoryResponse | undefined> {
        const { data, error } = await tryExecute(
            this.#host,
            client.get<VersionHistoryApiResponse>({
                url: `/umbraco/ai/management/api/v1/profiles/${profileId}/versions`,
                query: { skip, take },
                security: [{ scheme: "bearer", type: "http" }],
            })
        );

        if (error || !data) {
            console.error("Failed to load profile version history:", error);
            return undefined;
        }

        return this.#mapToVersionHistoryResponse(data);
    }

    /**
     * Compares two versions of a profile.
     * @param profileId - The profile ID.
     * @param fromVersion - The source version number.
     * @param toVersion - The target version number.
     * @returns The comparison response with property changes.
     */
    async compareVersions(
        profileId: string,
        fromVersion: number,
        toVersion: number
    ): Promise<UaiVersionComparisonResponse | undefined> {
        const { data, error } = await tryExecute(
            this.#host,
            client.get<VersionComparisonApiResponse>({
                url: `/umbraco/ai/management/api/v1/profiles/${profileId}/versions/compare`,
                query: { from: fromVersion, to: toVersion },
                security: [{ scheme: "bearer", type: "http" }],
            })
        );

        if (error || !data) {
            console.error("Failed to compare profile versions:", error);
            return undefined;
        }

        return this.#mapToComparisonResponse(data);
    }

    /**
     * Rolls back a profile to a previous version.
     * @param profileId - The profile ID.
     * @param version - The version number to rollback to.
     * @returns True if rollback was successful.
     */
    async rollback(profileId: string, version: number): Promise<boolean> {
        const { error } = await tryExecute(
            this.#host,
            client.post({
                url: `/umbraco/ai/management/api/v1/profiles/${profileId}/rollback/${version}`,
                security: [{ scheme: "bearer", type: "http" }],
            })
        );

        if (error) {
            console.error("Failed to rollback profile:", error);
            return false;
        }

        return true;
    }

    #mapToVersionHistoryResponse(data: VersionHistoryApiResponse): UaiVersionHistoryResponse {
        return {
            currentVersion: data.currentVersion,
            totalVersions: data.totalVersions,
            versions: data.versions.map((v): UaiVersionHistoryItem => ({
                id: v.id,
                entityId: v.entityId,
                version: v.version,
                dateCreated: v.dateCreated,
                createdByUserId: v.createdByUserId,
                createdByUserName: v.createdByUserName,
                changeDescription: v.changeDescription,
            })),
        };
    }

    #mapToComparisonResponse(data: VersionComparisonApiResponse): UaiVersionComparisonResponse {
        return {
            fromVersion: data.fromVersion,
            toVersion: data.toVersion,
            changes: data.changes.map((c): UaiVersionPropertyChange => ({
                propertyName: c.propertyName,
                oldValue: c.oldValue,
                newValue: c.newValue,
            })),
        };
    }
}
