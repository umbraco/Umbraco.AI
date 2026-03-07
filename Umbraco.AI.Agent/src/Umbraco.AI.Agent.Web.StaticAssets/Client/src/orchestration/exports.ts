/**
 * Public API exports for the orchestration module.
 * Only types and constants that should be consumed by other packages.
 */

// Entity types and constants
export { UAI_ORCHESTRATION_ENTITY_TYPE, UAI_ORCHESTRATION_ROOT_ENTITY_TYPE } from "./entity.js";
export type { UaiOrchestrationEntityType, UaiOrchestrationRootEntityType } from "./entity.js";

// Domain types
export type {
    UaiOrchestrationDetailModel,
    UaiOrchestrationItemModel,
    UaiOrchestrationGraph,
    UaiOrchestrationNode,
    UaiOrchestrationNodeConfig,
    UaiOrchestrationEdge,
    UaiOrchestrationRouteCondition,
} from "./types.js";

// Repository constants
export {
    UAI_ORCHESTRATION_DETAIL_REPOSITORY_ALIAS,
    UAI_ORCHESTRATION_DETAIL_STORE_ALIAS,
    UAI_ORCHESTRATION_COLLECTION_REPOSITORY_ALIAS,
} from "./repository/constants.js";

// Workspace constants
export {
    UAI_ORCHESTRATION_WORKSPACE_ALIAS,
    UAI_ORCHESTRATION_ROOT_WORKSPACE_ALIAS,
} from "./workspace/constants.js";

// Collection constants
export { UAI_ORCHESTRATION_COLLECTION_ALIAS } from "./collection/constants.js";

// Orchestration icon
export { UAI_ORCHESTRATION_ICON } from "./constants.js";
