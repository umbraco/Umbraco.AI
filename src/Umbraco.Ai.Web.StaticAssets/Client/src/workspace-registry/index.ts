/**
 * Workspace Registry Module
 *
 * Provides cross-DOM access to workspace contexts for AI tools.
 *
 * @example
 * ```typescript
 * import { workspaceRegistry, initWorkspaceDecorator } from '@umbraco-ai/core/workspace-registry';
 *
 * // Initialize in entrypoint (once)
 * initWorkspaceDecorator(extensionRegistry);
 *
 * // Get all active workspaces (e.g., document + block)
 * const workspaces = workspaceRegistry.getAll();
 *
 * // Get specific workspace by entity
 * const doc = workspaceRegistry.getByEntity('document', documentGuid);
 *
 * // Subscribe to changes
 * workspaceRegistry.changes$.subscribe((event) => {
 *   console.log(`Workspace ${event.type}: ${event.key}`);
 * });
 * ```
 */

// Public API
export { workspaceRegistry } from "./workspace.registry.js";
export { initWorkspaceDecorator } from "./workspace.decorator.js";

// Types
export type { WorkspaceEntry, WorkspaceChangeEvent } from "./types.js";
