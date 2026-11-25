/** The `onBehalfOf` kind that resolves an Automate workspace's service account. */
export const UAI_ON_BEHALF_OF_AUTOMATE_WORKSPACE_KIND = "automate-workspace";

/**
 * Builds the `onBehalfOf` query value for an Automate workspace, or undefined when no workspace is set.
 */
export function buildAutomateWorkspaceOnBehalfOf(workspaceId: string | undefined): string | undefined {
    return workspaceId ? `${UAI_ON_BEHALF_OF_AUTOMATE_WORKSPACE_KIND}:${workspaceId}` : undefined;
}
