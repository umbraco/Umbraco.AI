import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";

/**
 * AuditLog status enum matching backend AIAuditLogStatus
 */
export type UaiAuditLogStatus = "Running" | "Succeeded" | "Failed" | "Cancelled" | "PartialSuccess";

/**
 * Lightweight model for trace items in collection views.
 * Maps from API's AuditLogItemResponseModel.
 */
export interface UaiAuditLogItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    startTime: string;
    durationMs: number | null;
    status: UaiAuditLogStatus;
    userId: string;
    userName: string | null;
    entityId: string | null;
    profileId: string;
    profileAlias: string;
    profileVersion: string;
    modelId: string;
    providerId: string;
    featureType: string | null;
    featureId: string | null;
    featureVersion: string | null;
    inputTokens: number | null;
    outputTokens: number | null;
    errorMessage: string | null;
}

/**
 * Detailed model for trace detail views.
 * Maps from API's AuditLogDetailResponseModel.
 */
export interface UaiAuditLogDetailModel extends UaiAuditLogItemModel {
    endTime: string | null;
    entityTypeDetail: string | null;  // Renamed to avoid conflict with base type
    profileId: string;
    profileAlias: string;
    errorCategory: string | null;
    totalTokens: number | null;
    promptSnapshot: string | null;
    responseSnapshot: string | null;
    detailLevel: string;
    metadata: Array<{ key: string, value?:string | null}> | null;
}

/**
 * Filter model for querying traces.
 * Maps to API's AuditLogFilterRequestModel.
 */
export interface UaiAuditLogFilter {
    status?: string;
    userId?: string;
    profileId?: string;
    providerId?: string;
    entityId?: string;
    fromDate?: string;
    toDate?: string;
    searchText?: string;
}
