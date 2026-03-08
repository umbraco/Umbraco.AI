import type { UmbEntityModel } from "@umbraco-cms/backoffice/entity";
import type { UaiAgentScope } from "../agent/types.js";

/**
 * Graph model for orchestration workflow.
 */
export interface UaiOrchestrationGraph {
    nodes: UaiOrchestrationNode[];
    edges: UaiOrchestrationEdge[];
}

/**
 * Node in the orchestration graph.
 */
export interface UaiOrchestrationNode {
    id: string;
    type: string;
    label: string;
    x: number;
    y: number;
    config: UaiOrchestrationNodeConfig;
}

// ── Per-node-type config interfaces ─────────────────────────────────────

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

/**
 * Routing condition for Router nodes.
 */
export interface UaiOrchestrationRouteCondition {
    label: string;
    field: string;
    operator: string;
    value: string;
    targetNodeId: string;
}

/**
 * Edge connecting two nodes in the graph.
 */
export interface UaiOrchestrationEdge {
    id: string;
    sourceNodeId: string;
    targetNodeId: string;
    isDefault: boolean;
    priority?: number | null;
}

/**
 * Detail model for workspace editing.
 */
export interface UaiOrchestrationDetailModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description: string | null;
    profileId: string | null;
    surfaceIds: string[];
    scope: UaiAgentScope | null;
    graph: UaiOrchestrationGraph;
    isActive: boolean;
    dateCreated: string | null;
    dateModified: string | null;
    version: number;
}

/**
 * Collection item model (lighter weight for lists).
 */
export interface UaiOrchestrationItemModel extends UmbEntityModel {
    unique: string;
    entityType: string;
    alias: string;
    name: string;
    description: string | null;
    profileId: string | null;
    surfaceIds: string[];
    scope: UaiAgentScope | null;
    isActive: boolean;
    dateCreated: string | null;
    dateModified: string | null;
}
