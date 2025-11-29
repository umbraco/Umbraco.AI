import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";

/**
 * Model reference for AI models.
 */
export interface UaiModelRef {
    providerId: string;
    modelId: string;
}

/**
 * Detail model for workspace editing.
 */
export interface UaiProfileDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    capability: string;
    model: UaiModelRef | null;
    connectionId: string;
    temperature: number | null;
    maxTokens: number | null;
    systemPromptTemplate: string | null;
    tags: string[];
}

/**
 * Collection item model (lighter weight for lists).
 */
export interface UaiProfileItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    capability: string;
    model: UaiModelRef | null;
}
