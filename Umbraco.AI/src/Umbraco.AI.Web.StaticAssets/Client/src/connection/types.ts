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

/**
 * Display tone of a model fact.
 * Maps from API's AiModelFactToneModel.
 */
export type UaiModelFactTone = "Neutral" | "Positive" | "Warning";

/**
 * A single fact about a model (e.g. context window, price) for UI consumption.
 * Maps from API's ModelFactResponseModel.
 */
export interface UaiModelFactModel {
    key: string;
    label: string;
    shortLabel?: string;
    value: string;
    sortValue?: number;
    detail?: string;
    tone: UaiModelFactTone;
    url?: string;
}

/**
 * The facts known for one model, for UI consumption.
 * Maps from API's ModelFactsItemResponseModel.
 */
export interface UaiModelFactsModel {
    model: UaiModelRefModel;
    facts: UaiModelFactModel[];
}
