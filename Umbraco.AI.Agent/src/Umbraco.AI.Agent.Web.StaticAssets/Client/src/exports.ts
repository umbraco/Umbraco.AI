/**
 * Public API exports for @umbraco-ai/agent package.
 *
 * This file defines the stable public API that consuming packages can depend on.
 * Internal implementation details are NOT exported here.
 */

/**
 * Public API exports for @umbraco-ai/agent package.
 *
 * IMPORTANT: API types (AgentResponseModel, CreateAgentRequestModel, etc.) and services
 * (AgentsService) are NOT exported - they are internal implementation details.
 *
 * Consumers should use:
 * - Domain types: UaiAgentDetailModel, UaiAgentItemModel (from agent/exports.js)
 * - Repositories: UaiAgentsRepository (from agent/exports.js)
 * - Transport types and clients (from transport/exports.js)
 */

export * from './agent/exports.js';
export * from './transport/exports.js';
