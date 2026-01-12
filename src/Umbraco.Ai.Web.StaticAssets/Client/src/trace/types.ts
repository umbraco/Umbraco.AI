import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";

/**
 * Trace status enum matching backend AiTraceStatus
 */
export type UaiTraceStatus = "Running" | "Succeeded" | "Failed" | "Cancelled" | "PartialSuccess";

/**
 * Execution span status enum matching backend AiExecutionSpanStatus
 */
export type UaiExecutionSpanStatus = "Running" | "Succeeded" | "Failed" | "Skipped";

/**
 * Lightweight model for trace items in collection views.
 * Maps from API's TraceItemResponseModel.
 */
export interface UaiTraceItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    traceId: string;
    startTime: string;
    durationMs: number | null;
    status: UaiTraceStatus;
    userId: string;
    userName: string | null;
    entityId: string | null;
    operationType: string;
    modelId: string;
    providerId: string;
    featureType: string | null;
    featureId: string | null;
    inputTokens: number | null;
    outputTokens: number | null;
    errorMessage: string | null;
}

/**
 * Detailed model for trace detail views.
 * Maps from API's TraceDetailResponseModel.
 */
export interface UaiTraceDetailModel extends UaiTraceItemModel {
    spanId: string;
    endTime: string | null;
    entityTypeDetail: string | null;  // Renamed to avoid conflict with base type
    profileId: string;
    profileAlias: string;
    errorCategory: string | null;
    totalTokens: number | null;
    promptSnapshot: string | null;
    responseSnapshot: string | null;
    detailLevel: string;
    hasSpans: boolean;
}

/**
 * Model for execution spans within a trace.
 * Maps from API's ExecutionSpanResponseModel.
 */
export interface UaiExecutionSpanModel {
    id: string;
    traceId: string;
    spanId: string;
    parentSpanId: string | null;
    spanName: string;
    spanType: string;
    sequenceNumber: number;
    startTime: string;
    endTime: string | null;
    durationMs: number | null;
    status: UaiExecutionSpanStatus;
    inputData: string | null;
    outputData: string | null;
    errorData: string | null;
    retryCount: number | null;
    tokensUsed: number | null;
}

/**
 * Filter model for querying traces.
 * Maps to API's TraceFilterRequestModel.
 */
export interface UaiTraceFilter {
    status?: string;
    userId?: string;
    profileId?: string;
    providerId?: string;
    entityId?: string;
    fromDate?: string;
    toDate?: string;
    searchText?: string;
}
