/**
 * Public API exports for the agent module.
 * Only types and constants that should be consumed by other packages.
 */

// Entity types and constants
export { UAI_AGENT_ENTITY_TYPE, UAI_AGENT_ROOT_ENTITY_TYPE } from './entity.js';
export type { UaiAgentEntityType, UaiAgentRootEntityType } from './entity.js';

// Domain types
export type { UaiAgentDetailModel, UaiAgentItemModel } from './types.js';

// Repository constants
export { UAI_AGENT_DETAIL_REPOSITORY_ALIAS, UAI_AGENT_DETAIL_STORE_ALIAS, UAI_AGENT_COLLECTION_REPOSITORY_ALIAS } from './repository/constants.js';

// Workspace constants
export { UAI_AGENT_WORKSPACE_ALIAS, UAI_AGENT_ROOT_WORKSPACE_ALIAS } from './workspace/constants.js';

// Collection constants
export { UAI_AGENT_COLLECTION_ALIAS } from './collection/constants.js';

// Agent icon
export { UAI_AGENT_ICON } from './constants.js';
