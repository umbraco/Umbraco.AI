import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";

/**
 * Detail model for workspace editing.
 */
export interface UaiAgentDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description: string | null;
    profileId: string;
    contextIds: string[];
    instructions: string | null;
    isActive: boolean;
}

/**
 * Collection item model (lighter weight for lists).
 */
export interface UaiAgentItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description: string | null;
    profileId: string;
    contextIds: string[];
    isActive: boolean;
}
