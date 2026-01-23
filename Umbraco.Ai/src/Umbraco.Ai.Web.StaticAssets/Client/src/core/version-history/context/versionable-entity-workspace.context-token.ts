import type { UaiVersionableEntityWorkspaceContext } from "./versionable-entity-workspace.context.js";

/**
 * Type guard to check if a workspace context supports version history.
 * @param context - The workspace context to check.
 * @returns True if the context supports version history.
 */
export function isVersionableEntityWorkspaceContext(
    context: unknown
): context is UaiVersionableEntityWorkspaceContext {
    return (
        context !== null &&
        typeof context === "object" &&
        "version" in context &&
        "getVersionHistory" in context &&
        "compareVersions" in context &&
        "rollbackToVersion" in context
    );
}
