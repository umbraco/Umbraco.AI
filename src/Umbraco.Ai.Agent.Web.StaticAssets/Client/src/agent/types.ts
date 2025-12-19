import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";
import type { UAiAgentScope } from "./property-actions/types.js";

/**
 * Detail model for workspace editing.
 */
export interface UAiAgentDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description: string | null;
    content: string;
    profileId: string | null;
    tags: string[];
    scope: UAiAgentScope | null;
    isActive: boolean;
}

/**
 * Collection item model (lighter weight for lists).
 */
export interface UAiAgentItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description: string | null;
    isActive: boolean;
}
