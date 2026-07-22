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

/** Deep link to a project within the workspace. */
export function copilotWorkspaceProjectPath(projectId: string): string {
    return `${UAI_COPILOT_WORKSPACE_SECTION_PATH}/project/${encodeURIComponent(projectId)}`;
}

/** Deep link to create a new project. */
export function copilotWorkspaceProjectCreatePath(): string {
    return `${UAI_COPILOT_WORKSPACE_SECTION_PATH}/project/create`;
}
