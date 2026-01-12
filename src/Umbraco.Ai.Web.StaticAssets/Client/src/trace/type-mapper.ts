// TODO: Once API types are regenerated, replace 'any' with proper imports:
// import type { TraceItemResponseModel, TraceDetailResponseModel, ExecutionSpanResponseModel } from "../api/types.gen.js";

import { UAI_TRACE_ENTITY_TYPE } from "./entity.js";
import type { UaiTraceItemModel, UaiTraceDetailModel, UaiExecutionSpanModel, UaiTraceStatus, UaiExecutionSpanStatus } from "./types.js";

/**
 * Type mapper for converting API response models to UI models.
 */
export const UaiTraceTypeMapper = {
    /**
     * Maps TraceItemResponseModel to UaiTraceItemModel for collection views.
     */
    toItemModel(response: any): UaiTraceItemModel {
        return {
            unique: response.id,
            entityType: UAI_TRACE_ENTITY_TYPE,
            traceId: response.traceId,
            startTime: response.startTime,
            durationMs: response.durationMs ?? null,
            status: response.status as UaiTraceStatus,
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
     * Maps TraceDetailResponseModel to UaiTraceDetailModel for detail views.
     */
    toDetailModel(response: any): UaiTraceDetailModel {
        return {
            unique: response.id,
            entityType: UAI_TRACE_ENTITY_TYPE,
            traceId: response.traceId,
            spanId: response.spanId,
            startTime: response.startTime,
            endTime: response.endTime ?? null,
            durationMs: response.durationMs ?? null,
            status: response.status as UaiTraceStatus,
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
            hasSpans: response.hasSpans,
        };
    },

    /**
     * Maps ExecutionSpanResponseModel to UaiExecutionSpanModel.
     */
    toSpanModel(response: any): UaiExecutionSpanModel {
        return {
            id: response.id,
            traceId: response.traceId,
            spanId: response.spanId,
            parentSpanId: response.parentSpanId ?? null,
            spanName: response.spanName,
            spanType: response.spanType,
            sequenceNumber: response.sequenceNumber,
            startTime: response.startTime,
            endTime: response.endTime ?? null,
            durationMs: response.durationMs ?? null,
            status: response.status as UaiExecutionSpanStatus,
            inputData: response.inputData ?? null,
            outputData: response.outputData ?? null,
            errorData: response.errorData ?? null,
            retryCount: response.retryCount ?? null,
            tokensUsed: response.tokensUsed ?? null,
        };
    },
};
