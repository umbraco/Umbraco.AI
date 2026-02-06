import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";
import type { UaiUserGroupPermissionsMap } from "./user-group-permissions.js";

/**
 * Detail model for workspace editing.
 */
export interface UaiAgentDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description: string | null;
    profileId: string | null;
    contextIds: string[];
    scopeIds: string[];
    allowedToolIds: string[];
    allowedToolScopeIds: string[];
    userGroupPermissions: UaiUserGroupPermissionsMap;
    instructions: string | null;
    isActive: boolean;
    dateCreated: string | null;
    dateModified: string | null;
    version: number;
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
    profileId: string | null;
    contextIds: string[];
    scopeIds: string[];
    allowedToolIds: string[];
    allowedToolScopeIds: string[];
    isActive: boolean;
    dateCreated: string | null;
    dateModified: string | null;
}
