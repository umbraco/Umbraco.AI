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
    config: UaiNodeConfig;
}

// ── Per-node-type configs (polymorphic via $type) ───────────────────────

export interface UaiStartNodeConfig {
    $type: "start";
}

export interface UaiEndNodeConfig {
    $type: "end";
}

export interface UaiAgentNodeConfig {
    $type: "agent";
    agentId?: string | null;
    isManager?: boolean;
}

export interface UaiToolCallNodeConfig {
    $type: "toolCall";
    toolId?: string | null;
}

export interface UaiRouterNodeConfig {
    $type: "router";
}

export interface UaiAggregatorNodeConfig {
    $type: "aggregator";
    aggregationStrategy?: string | null;
    profileId?: string | null;
}

export interface UaiCommunicationBusNodeConfig {
    $type: "communicationBus";
    maxIterations?: number;
    terminationMessage?: string | null;
}

/**
 * Union of all node config types, discriminated by $type.
 */
export type UaiNodeConfig =
    | UaiStartNodeConfig
    | UaiEndNodeConfig
    | UaiAgentNodeConfig
    | UaiToolCallNodeConfig
    | UaiRouterNodeConfig
    | UaiAggregatorNodeConfig
    | UaiCommunicationBusNodeConfig;

// ── Node config type guards ─────────────────────────────────────────────

export function isAgentNodeConfig(config: UaiNodeConfig): config is UaiAgentNodeConfig {
    return config.$type === "agent";
}

export function isToolCallNodeConfig(config: UaiNodeConfig): config is UaiToolCallNodeConfig {
    return config.$type === "toolCall";
}

export function isAggregatorNodeConfig(config: UaiNodeConfig): config is UaiAggregatorNodeConfig {
    return config.$type === "aggregator";
}

export function isCommunicationBusNodeConfig(config: UaiNodeConfig): config is UaiCommunicationBusNodeConfig {
    return config.$type === "communicationBus";
}

// ── Node config factory ─────────────────────────────────────────────────

export function createDefaultNodeConfig(nodeType: string): UaiNodeConfig {
    switch (nodeType) {
        case "Start": return { $type: "start" };
        case "End": return { $type: "end" };
        case "Agent": return { $type: "agent" };
        case "ToolCall": return { $type: "toolCall" };
        case "Router": return { $type: "router" };
        case "Aggregator": return { $type: "aggregator", aggregationStrategy: "Concat" };
        case "CommunicationBus": return { $type: "communicationBus", maxIterations: 40 };
        default: return { $type: "start" };
    }
}

// ── Edge types ──────────────────────────────────────────────────────────

export interface UaiOrchestrationRouteCondition {
    label: string;
    field: string;
    operator: string;
    value: string;
}

export interface UaiOrchestrationEdge {
    id: string;
    sourceNodeId: string;
    targetNodeId: string;
    isDefault: boolean;
    priority?: number | null;
    condition?: UaiOrchestrationRouteCondition | null;
    requiresApproval?: boolean;
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
