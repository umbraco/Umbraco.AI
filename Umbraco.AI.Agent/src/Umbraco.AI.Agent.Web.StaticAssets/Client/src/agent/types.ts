import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";
import type { UaiUserGroupPermissionsMap } from "./user-group-permissions.js";

/**
 * Rule for agent scope filtering.
 * All non-null/non-empty properties use AND logic between them.
 * Values within each array use OR logic.
 */
export interface UaiAgentScopeRule {
    sections?: string[] | null;
    entityTypes?: string[] | null;
}

/**
 * Defines where an agent is available using allow and deny rules.
 */
export interface UaiAgentScope {
    allowRules: UaiAgentScopeRule[];
    denyRules: UaiAgentScopeRule[];
}

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
    surfaceIds: string[];
    scope: UaiAgentScope | null;
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
    surfaceIds: string[];
    scope: UaiAgentScope | null;
    allowedToolIds: string[];
    allowedToolScopeIds: string[];
    isActive: boolean;
    dateCreated: string | null;
    dateModified: string | null;
}
