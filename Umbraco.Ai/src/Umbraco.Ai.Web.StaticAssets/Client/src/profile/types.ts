import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";

/**
 * Model reference for AI models.
 */
export interface UaiModelRef {
    providerId: string;
    modelId: string;
}

/**
 * Base interface for profile settings.
 */
export interface UaiProfileSettings {
    $type: string;
}

/**
 * Chat-specific profile settings.
 */
export interface UaiChatProfileSettings extends UaiProfileSettings {
    $type: "chat";
    temperature: number | null;
    maxTokens: number | null;
    systemPromptTemplate: string | null;
    contextIds: string[];
}

/**
 * Embedding-specific profile settings.
 */
export interface UaiEmbeddingProfileSettings extends UaiProfileSettings {
    $type: "embedding";
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
    settings: UaiProfileSettings | null;
    tags: string[];
    dateCreated: string | null;
    dateModified: string | null;
    version: number;
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
    dateModified: string | null;
}

/**
 * Type guard for chat settings.
 */
export function isChatSettings(settings: UaiProfileSettings | null): settings is UaiChatProfileSettings {
    return settings?.$type === "chat";
}

/**
 * Type guard for embedding settings.
 */
export function isEmbeddingSettings(settings: UaiProfileSettings | null): settings is UaiEmbeddingProfileSettings {
    return settings?.$type === "embedding";
}
