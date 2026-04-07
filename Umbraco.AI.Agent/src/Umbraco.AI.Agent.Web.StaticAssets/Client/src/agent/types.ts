import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";
import type { UaiUserGroupPermissionsMap } from "./user-group-permissions.js";

// ── Agent type discriminator ────────────────────────────────────────────

export type UaiAgentType = "standard" | "orchestrated";

// ── Scope ───────────────────────────────────────────────────────────────

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

// ── Polymorphic config ──────────────────────────────────────────────────

export interface UaiStandardAgentConfig {
    $type: "standard";
    contextIds: string[];
    instructions: string | null;
    allowedToolIds: string[];
    allowedToolScopeIds: string[];
    outputSchema: Record<string, unknown> | null;
    userGroupPermissions: UaiUserGroupPermissionsMap;
}

export interface UaiOrchestratedAgentConfig {
    $type: "orchestrated";
    workflowId: string | null;
    settings: unknown | null;
}

export type UaiAgentConfig = UaiStandardAgentConfig | UaiOrchestratedAgentConfig;

// ── Type guards ─────────────────────────────────────────────────────────

export function isStandardConfig(config: UaiAgentConfig | null | undefined): config is UaiStandardAgentConfig {
    return config?.$type === "standard";
}

export function isOrchestratedConfig(config: UaiAgentConfig | null | undefined): config is UaiOrchestratedAgentConfig {
    return config?.$type === "orchestrated";
}

export function isStandardAgent(model: { agentType: UaiAgentType }): boolean {
    return model.agentType === "standard";
}

export function isOrchestratedAgent(model: { agentType: UaiAgentType }): boolean {
    return model.agentType === "orchestrated";
}

// ── Workflow types ──────────────────────────────────────────────────────

/**
 * Represents a registered workflow available for orchestrated agents.
 */
export interface UaiWorkflowItem {
    id: string;
    name: string;
    description: string | null;
    settingsSchema: unknown | null;
}

// ── Detail and item models ──────────────────────────────────────────────

/**
 * Detail model for workspace editing.
 */
export interface UaiAgentDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description: string | null;
    agentType: UaiAgentType;
    profileId: string | null;
    surfaceIds: string[];
    scope: UaiAgentScope | null;
    config: UaiAgentConfig;
    guardrailIds: string[];
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
    agentType: UaiAgentType;
    profileId: string | null;
    surfaceIds: string[];
    scope: UaiAgentScope | null;
    guardrailIds: string[];
    isActive: boolean;
    dateCreated: string | null;
    dateModified: string | null;
}
