// TODO: Once API types are regenerated, replace 'any' with proper imports:
// import type { AuditLogItemResponseModel, AuditLogDetailResponseModel, AuditLogActivityResponseModel } from "../api/types.gen.js";

import { UAI_AUDIT_LOG_ENTITY_TYPE } from "./entity.js";
import {
    UaiAuditLogItemModel,
    UaiAuditLogDetailModel,
    UaiAuditLogStatus,
} from "./types.js";

/**
 * Type mapper for converting API response models to UI models.
 */
export const UaiAuditLogTypeMapper = {
    /**
     * Maps AuditLogItemResponseModel to UaiAuditLogItemModel for collection views.
     */
    toItemModel(response: any): UaiAuditLogItemModel {
        return {
            unique: response.id,
            entityType: UAI_AUDIT_LOG_ENTITY_TYPE,
            startTime: response.startTime,
            durationMs: response.durationMs ?? null,
            status: response.status as UaiAuditLogStatus,
            userId: response.userId,
            userName: response.userName ?? null,
            entityId: response.entityId ?? null,
            profileId: response.profileId,
            profileAlias: response.profileAlias,
            profileVersion: response.profileVersion ?? null,
            providerId: response.providerId,
            modelId: response.modelId,
            featureType: response.featureType ?? null,
            featureId: response.featureId ?? null,
            featureVersion: response.featureVersion ?? null,
            inputTokens: response.inputTokens ?? null,
            outputTokens: response.outputTokens ?? null,
            errorMessage: response.errorMessage ?? null,
        };
    },

    /**
     * Maps AuditLogDetailResponseModel to UaiAuditLogDetailModel for detail views.
     */
    toDetailModel(response: any): UaiAuditLogDetailModel {
        return {
            unique: response.id,
            entityType: UAI_AUDIT_LOG_ENTITY_TYPE,
            startTime: response.startTime,
            endTime: response.endTime ?? null,
            durationMs: response.durationMs ?? null,
            status: response.status as UaiAuditLogStatus,
            userId: response.userId,
            userName: response.userName ?? null,
            entityId: response.entityId ?? null,
            entityTypeDetail: response.entityType ?? null,
            modelId: response.modelId,
            providerId: response.providerId,
            featureType: response.featureType ?? null,
            featureId: response.featureId ?? null,
            featureVersion: response.featureVersion ?? null,
            profileId: response.profileId,
            profileAlias: response.profileAlias,
            profileVersion: response.profileVersion ?? null,
            inputTokens: response.inputTokens ?? null,
            outputTokens: response.outputTokens ?? null,
            totalTokens: response.totalTokens ?? null,
            errorMessage: response.errorMessage ?? null,
            errorCategory: response.errorCategory ?? null,
            promptSnapshot: response.promptSnapshot ?? null,
            responseSnapshot: response.responseSnapshot ?? null,
            detailLevel: response.detailLevel,
            metadata: response.metadata ?? null,
        };
    },
};
