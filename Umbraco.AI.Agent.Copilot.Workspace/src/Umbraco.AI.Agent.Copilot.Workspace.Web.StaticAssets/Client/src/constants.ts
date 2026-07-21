/**
 * The Copilot Workspace backoffice section alias. Must match the backend section
 * (`CopilotWorkspaceConstants.Sections.CopilotWorkspace`) so a user granted the section
 * (see the AddCopilotWorkspaceSectionToAdminGroup migration) can see and enter it.
 */
export const UAI_COPILOT_WORKSPACE_SECTION_ALIAS = "aiCopilotWorkspace";

/** URL path segment for the section. */
export const UAI_COPILOT_WORKSPACE_SECTION_PATHNAME = "copilot-workspace";

/** The management API OpenAPI document name (matches the backend `[MapToApi]`). */
export const UAI_COPILOT_WORKSPACE_API_NAME = "ai-copilot-workspace-management";
