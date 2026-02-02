import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";

// View model for workspace editing
export interface UaiConnectionDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    providerId: string;
    settings: Record<string, unknown> | null;
    isActive: boolean;
    version: number;
    dateCreated: string | null;
    dateModified: string | null;
}

// Collection item model
export interface UaiConnectionItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    providerId: string;
    isActive: boolean;
    dateModified: string | null;
}

/**
 * Model reference for UI consumption.
 * Maps from API's ModelRefModel.
 */
export interface UaiModelRefModel {
    providerId: string;
    modelId: string;
}

/**
 * Model descriptor for UI consumption.
 * Maps from API's ModelDescriptorResponseModel.
 */
export interface UaiModelDescriptorModel {
    model: UaiModelRefModel;
    name: string;
    metadata?: Record<string, string>;
}
