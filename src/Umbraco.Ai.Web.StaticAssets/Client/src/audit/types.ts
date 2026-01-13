import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";

/**
 * Audit status enum matching backend AiAuditStatus
 */
export type UaiAuditStatus = "Running" | "Succeeded" | "Failed" | "Cancelled" | "PartialSuccess";

/**
 * Execution span status enum matching backend AiAuditActivityStatus
 */
export type UaiAuditActivityStatus = "Running" | "Succeeded" | "Failed" | "Skipped";

/**
 * Lightweight model for trace items in collection views.
 * Maps from API's AuditItemResponseModel.
 */
export interface UaiAuditItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    startTime: string;
    durationMs: number | null;
    status: UaiAuditStatus;
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
 * Maps from API's AuditDetailResponseModel.
 */
export interface UaiAuditDetailModel extends UaiAuditItemModel {
    endTime: string | null;
    entityTypeDetail: string | null;  // Renamed to avoid conflict with base type
    profileId: string;
    profileAlias: string;
    errorCategory: string | null;
    totalTokens: number | null;
    promptSnapshot: string | null;
    responseSnapshot: string | null;
    detailLevel: string;
    hasActivities: boolean;
}

/**
 * Model for execution spans within a trace.
 * Maps from API's UaiAuditActivityModel.
 */
export interface UaiAuditActivityModel {
    id: string;
    activityName: string;
    activityType: string;
    sequenceNumber: number;
    startTime: string;
    endTime: string | null;
    durationMs: number | null;
    status: UaiAuditActivityStatus;
    inputData: string | null;
    outputData: string | null;
    errorData: string | null;
    retryCount: number | null;
    tokensUsed: number | null;
}

/**
 * Filter model for querying traces.
 * Maps to API's AuditFilterRequestModel.
 */
export interface UaiAuditFilter {
    status?: string;
    userId?: string;
    profileId?: string;
    providerId?: string;
    entityId?: string;
    fromDate?: string;
    toDate?: string;
    searchText?: string;
}
