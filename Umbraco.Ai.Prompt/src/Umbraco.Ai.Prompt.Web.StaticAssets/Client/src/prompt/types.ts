import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";
import type { UaiPromptScope } from "./property-actions/types.js";

/**
 * Detail model for workspace editing.
 */
export interface UaiPromptDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description: string | null;
    instructions: string;
    profileId: string | null;
    contextIds: string[];
    tags: string[];
    scope: UaiPromptScope | null;
    isActive: boolean;
    includeEntityContext: boolean;
    dateCreated: string | null;
    dateModified: string | null;
    version: number;
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
    dateModified: string | null;
}
