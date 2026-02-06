import { EntityVersionHistoryResponseModel, EntityVersionComparisonResponseModel } from "../../api";
import type {
    UaiVersionComparisonResponse,
    UaiVersionHistoryItem,
    UaiVersionHistoryResponse,
    UaiVersionPropertyChange,
} from "./types.ts";

export const UaiVersionHistoryTypeMapper = {
    mapToVersionHistoryResponse(data: EntityVersionHistoryResponseModel): UaiVersionHistoryResponse {
        return {
            currentVersion: data.currentVersion,
            totalVersions: data.totalVersions,
            versions: data.versions.map(
                (v): UaiVersionHistoryItem => ({
                    id: v.id,
                    entityId: v.entityId,
                    version: v.version,
                    dateCreated: v.dateCreated,
                    createdByUserId: v.createdByUserId,
                    createdByUserName: v.createdByUserName,
                    changeDescription: v.changeDescription,
                }),
            ),
        };
    },

    mapToComparisonResponse(data: EntityVersionComparisonResponseModel): UaiVersionComparisonResponse {
        return {
            fromVersion: data.fromVersion,
            toVersion: data.toVersion,
            changes: data.changes.map(
                (c): UaiVersionPropertyChange => ({
                    propertyName: c.propertyName,
                    oldValue: c.oldValue,
                    newValue: c.newValue,
                }),
            ),
        };
    },
};
