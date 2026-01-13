// TODO: Once API types are regenerated, replace 'any' with proper imports:
// import type { AuditItemResponseModel, AuditDetailResponseModel, AuditActivityResponseModel } from "../api/types.gen.js";

import { UAI_AUDIT_ENTITY_TYPE } from "./entity.js";
import {
    UaiAuditItemModel,
    UaiAuditDetailModel,
    UaiAuditStatus,
    UaiAuditActivityModel,
    UaiAuditActivityStatus
} from "./types.js";

/**
 * Type mapper for converting API response models to UI models.
 */
export const UaiAuditTypeMapper = {
    /**
     * Maps AuditItemResponseModel to UaiAuditItemModel for collection views.
     */
    toItemModel(response: any): UaiAuditItemModel {
        return {
            unique: response.id,
            entityType: UAI_AUDIT_ENTITY_TYPE,
            startTime: response.startTime,
            durationMs: response.durationMs ?? null,
            status: response.status as UaiAuditStatus,
            userId: response.userId,
            userName: response.userName ?? null,
            entityId: response.entityId ?? null,
            operationType: response.operationType,
            modelId: response.modelId,
            providerId: response.providerId,
            featureType: response.featureType ?? null,
            featureId: response.featureId ?? null,
            inputTokens: response.inputTokens ?? null,
            outputTokens: response.outputTokens ?? null,
            errorMessage: response.errorMessage ?? null,
        };
    },

    /**
     * Maps AuditDetailResponseModel to UaiAuditDetailModel for detail views.
     */
    toDetailModel(response: any): UaiAuditDetailModel {
        return {
            unique: response.id,
            entityType: UAI_AUDIT_ENTITY_TYPE,
            startTime: response.startTime,
            endTime: response.endTime ?? null,
            durationMs: response.durationMs ?? null,
            status: response.status as UaiAuditStatus,
            userId: response.userId,
            userName: response.userName ?? null,
            entityId: response.entityId ?? null,
            entityTypeDetail: response.entityType ?? null,
            operationType: response.operationType,
            modelId: response.modelId,
            providerId: response.providerId,
            featureType: response.featureType ?? null,
            featureId: response.featureId ?? null,
            profileId: response.profileId,
            profileAlias: response.profileAlias,
            inputTokens: response.inputTokens ?? null,
            outputTokens: response.outputTokens ?? null,
            totalTokens: response.totalTokens ?? null,
            errorMessage: response.errorMessage ?? null,
            errorCategory: response.errorCategory ?? null,
            promptSnapshot: response.promptSnapshot ?? null,
            responseSnapshot: response.responseSnapshot ?? null,
            detailLevel: response.detailLevel,
            hasActivities: response.hasActivities,
        };
    },

    /**
     * Maps AuditActivityResponseModel to UaiAuditActivityModel.
     */
    toActivityModel(response: any): UaiAuditActivityModel {
        return {
            id: response.id,
            activityName: response.activityName,
            activityType: response.activityType,
            sequenceNumber: response.sequenceNumber,
            startTime: response.startTime,
            endTime: response.endTime ?? null,
            durationMs: response.durationMs ?? null,
            status: response.status as UaiAuditActivityStatus,
            inputData: response.inputData ?? null,
            outputData: response.outputData ?? null,
            errorData: response.errorData ?? null,
            retryCount: response.retryCount ?? null,
            tokensUsed: response.tokensUsed ?? null,
        };
    },
};
