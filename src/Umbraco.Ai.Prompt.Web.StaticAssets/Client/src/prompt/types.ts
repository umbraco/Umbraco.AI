import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";

/**
 * Detail model for workspace editing.
 */
export interface UaiPromptDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description: string | null;
    content: string;
    profileId: string | null;
    tags: string[];
    isActive: boolean;
}

/**
 * Collection item model (lighter weight for lists).
 */
export interface UaiPromptItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description: string | null;
    isActive: boolean;
}
