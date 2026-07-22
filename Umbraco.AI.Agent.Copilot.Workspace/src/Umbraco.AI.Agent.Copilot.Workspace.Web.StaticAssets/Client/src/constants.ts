/**
 * The Copilot Workspace backoffice section alias. Must match the backend section
 * (`CopilotWorkspaceConstants.Sections.CopilotWorkspace`) so a user granted the section
 * (see the AddCopilotWorkspaceSectionToAdminGroup migration) can see and enter it.
 */
export const UAI_COPILOT_WORKSPACE_SECTION_ALIAS = "Uai.Section.CopilotWorkspace";

/** URL path segment for the section. */
export const UAI_COPILOT_WORKSPACE_SECTION_PATHNAME = "copilot-workspace";

/** The management API OpenAPI document name (matches the backend `[MapToApi]`). */
export const UAI_COPILOT_WORKSPACE_API_NAME = "ai-copilot-workspace-management";

/**
 * Entity type for a Copilot Workspace project. Used with `UaiEntityActionEvent` on the shared action
 * event bus so the reactive project repository (and its observers, e.g. the sidebar tree) update on
 * create/update/delete without a manual reload.
 */
export const UAI_PROJECT_ENTITY_TYPE = "uai-copilot-workspace-project";

/** Workspace alias for the project entity workspace (matches its manifests' condition + editor). */
export const UAI_PROJECT_WORKSPACE_ALIAS = "Uai.CopilotWorkspace.Workspace.Project";

/**
 * Window event fired when a conversation is created/renamed/moved/etc. from anywhere in the section,
 * so the sidebar list can reload. Used as a lightweight cross-region signal because the list (sidebar)
 * and the chat (routed centre) live in sibling subtrees of the section shell.
 */
export const UAI_COPILOT_WORKSPACE_CONVERSATIONS_CHANGED_EVENT = "uai-copilot-workspace:conversations-changed";

/** Dispatches {@link UAI_COPILOT_WORKSPACE_CONVERSATIONS_CHANGED_EVENT} so the sidebar list refreshes. */
export function notifyCopilotWorkspaceConversationsChanged(): void {
    window.dispatchEvent(new CustomEvent(UAI_COPILOT_WORKSPACE_CONVERSATIONS_CHANGED_EVENT));
}
