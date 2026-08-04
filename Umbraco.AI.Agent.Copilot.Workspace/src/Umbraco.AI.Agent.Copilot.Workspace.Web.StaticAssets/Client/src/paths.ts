import { UMB_SECTION_PATH_PATTERN } from "@umbraco-cms/backoffice/section";
import { UAI_COPILOT_WORKSPACE_SECTION_PATHNAME } from "./constants.js";

/**
 * Absolute backoffice path to the Copilot Workspace section, e.g. `/section/copilot-workspace`.
 * The section is a standalone custom section element (not a dashboard), so its own
 * `<umb-router-slot>` mounts the conversation/project views directly beneath the section path.
 */
export const UAI_COPILOT_WORKSPACE_SECTION_PATH = UMB_SECTION_PATH_PATTERN.generateAbsolute({
    sectionName: UAI_COPILOT_WORKSPACE_SECTION_PATHNAME,
});

/** Deep link to an open conversation within the workspace. */
export function copilotWorkspaceConversationPath(conversationId: string): string {
    return `${UAI_COPILOT_WORKSPACE_SECTION_PATH}/conversation/${encodeURIComponent(conversationId)}`;
}

/**
 * Deep link to a *new* (not-yet-persisted) conversation. Opening this starts a draft: the conversation
 * is only created server-side when the user sends their first message (see the chat context's draft
 * handling), so navigating here and leaving persists nothing. An optional `projectId` pre-attaches the
 * draft to a project.
 */
export function copilotWorkspaceConversationCreatePath(projectId?: string): string {
    const base = `${UAI_COPILOT_WORKSPACE_SECTION_PATH}/conversation/create`;
    return projectId ? `${base}?projectId=${encodeURIComponent(projectId)}` : base;
}

/** Deep link to a project within the workspace. */
export function copilotWorkspaceProjectPath(projectId: string): string {
    return `${UAI_COPILOT_WORKSPACE_SECTION_PATH}/project/${encodeURIComponent(projectId)}`;
}

/** Deep link to create a new project. */
export function copilotWorkspaceProjectCreatePath(): string {
    return `${UAI_COPILOT_WORKSPACE_SECTION_PATH}/project/create`;
}

/**
 * Navigates the backoffice to an in-app path. The single place programmatic navigation happens, so all
 * callers behave identically. Uses `history.pushState` (the section's `<umb-router-slot>` resolves the
 * new location and the sidebar tracks it via the router's `navigationend` event — see
 * `UaiCopilotWorkspaceSidebarContext`); centralized here so that contract lives in one documented spot.
 */
export function navigateToWorkspacePath(path: string): void {
    window.history.pushState({}, "", path);
}
