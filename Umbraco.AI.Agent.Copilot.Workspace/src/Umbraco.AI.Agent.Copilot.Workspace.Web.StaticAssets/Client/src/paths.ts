import { UMB_DASHBOARD_PATH_PATTERN } from "@umbraco-cms/backoffice/dashboard";
import { UAI_COPILOT_WORKSPACE_SECTION_PATHNAME } from "./constants.js";

/**
 * The dashboard pathname that hosts the workspace shell (matches the dashboard
 * manifest's `meta.pathname`). The shell's `<umb-router-slot>` mounts the
 * conversation/project views beneath this path.
 */
export const UAI_COPILOT_WORKSPACE_DASHBOARD_PATHNAME = "workspace";

/**
 * Absolute backoffice path to the workspace shell, e.g.
 * `/section/copilot-workspace/dashboard/workspace`. Built from the CMS path
 * patterns so it tracks the section/dashboard route structure rather than a
 * hardcoded string. Used as the base for conversation/project deep links.
 */
export const UAI_COPILOT_WORKSPACE_DASHBOARD_PATH = UMB_DASHBOARD_PATH_PATTERN.generateAbsolute({
    sectionName: UAI_COPILOT_WORKSPACE_SECTION_PATHNAME,
    dashboardPathname: UAI_COPILOT_WORKSPACE_DASHBOARD_PATHNAME,
});

/** Deep link to an open conversation within the workspace shell. */
export function copilotWorkspaceConversationPath(conversationId: string): string {
    return `${UAI_COPILOT_WORKSPACE_DASHBOARD_PATH}/conversation/${encodeURIComponent(conversationId)}`;
}

/** Deep link to a project within the workspace shell. */
export function copilotWorkspaceProjectPath(projectId: string): string {
    return `${UAI_COPILOT_WORKSPACE_DASHBOARD_PATH}/project/${encodeURIComponent(projectId)}`;
}
