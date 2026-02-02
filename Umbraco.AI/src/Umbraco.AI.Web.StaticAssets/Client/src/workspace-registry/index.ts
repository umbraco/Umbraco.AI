/**
 * Workspace Registry Module
 *
 * Provides cross-DOM access to workspace contexts for AI tools.
 *
 * @example
 * ```typescript
 * import { UAI_WORKSPACE_REGISTRY_CONTEXT } from '@umbraco-ai/core';
 *
 * // Consume in a component
 * this.consumeContext(UAI_WORKSPACE_REGISTRY_CONTEXT, (registry) => {
 *   // Get all active workspaces
 *   const workspaces = registry.getAll();
 *
 *   // Get specific workspace by entity
 *   const doc = registry.getByEntity('document', documentGuid);
 *
 *   // Subscribe to changes
 *   registry.changes$.subscribe((event) => {
 *     console.log(`Workspace ${event.type}: ${event.key}`);
 *   });
 * });
 * ```
 */

// Context and token
export {
	UaiWorkspaceRegistryContext,
	UAI_WORKSPACE_REGISTRY_CONTEXT,
} from "./workspace-registry.context.js";

// Types
export type { WorkspaceEntry, WorkspaceChangeEvent } from "./types.js";
