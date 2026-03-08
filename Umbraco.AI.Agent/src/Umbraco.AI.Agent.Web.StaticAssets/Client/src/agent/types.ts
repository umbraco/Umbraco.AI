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
    userGroupPermissions: UaiUserGroupPermissionsMap;
}

export interface UaiOrchestratedAgentConfig {
    $type: "orchestrated";
    graph: UaiOrchestrationGraph;
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

// ── Orchestration graph types ───────────────────────────────────────────

export interface UaiOrchestrationGraph {
    nodes: UaiOrchestrationNode[];
    edges: UaiOrchestrationEdge[];
}

export interface UaiOrchestrationNode {
    id: string;
    type: string;
    label: string;
    x: number;
    y: number;
    config: UaiOrchestrationNodeConfig;
}

export interface UaiAgentNodeConfig {
    agentId?: string | null;
}

export interface UaiFunctionNodeConfig {
    toolIds?: string[];
    toolName?: string | null;
}

export interface UaiRouterNodeConfig {
    conditions?: UaiOrchestrationRouteCondition[] | null;
}

export interface UaiAggregatorNodeConfig {
    aggregationStrategy?: string | null;
}

export interface UaiManagerNodeConfig {
    managerInstructions?: string | null;
    managerProfileId?: string | null;
}

/**
 * Union of all node config types.
 * Start and End nodes use an empty config object.
 */
export type UaiOrchestrationNodeConfig =
    | UaiAgentNodeConfig
    | UaiFunctionNodeConfig
    | UaiRouterNodeConfig
    | UaiAggregatorNodeConfig
    | UaiManagerNodeConfig
    | Record<string, never>;

export interface UaiOrchestrationRouteCondition {
    label: string;
    field: string;
    operator: string;
    value: string;
    targetNodeId: string;
}

export interface UaiOrchestrationEdge {
    id: string;
    sourceNodeId: string;
    targetNodeId: string;
    isDefault: boolean;
    priority?: number | null;
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
    isActive: boolean;
    dateCreated: string | null;
    dateModified: string | null;
}
