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
 * Entity types for Copilot Workspace, `uai:`-namespaced to match the AI section convention
 * (`uai:context`, `uai:profile`, …). Used with `UaiEntityActionEvent` on the shared action event bus
 * so reactive repositories and their observers (e.g. the sidebar tree) update on create/update/delete
 * without a manual reload, and as `forEntityTypes` targets for the entity actions.
 */
export const UAI_PROJECT_ENTITY_TYPE = "uai:copilot-workspace-project";
export const UAI_CONVERSATION_ENTITY_TYPE = "uai:copilot-workspace-conversation";

/**
 * Root/collection entity types (mirroring the CMS `…-root` convention). The section root hosts the
 * top-level "New chat" create action; the project root hosts "New project" (on the Projects sidebar
 * group's entity-action header).
 */
export const UAI_COPILOT_WORKSPACE_ROOT_ENTITY_TYPE = "uai:copilot-workspace-root";
export const UAI_PROJECT_ROOT_ENTITY_TYPE = "uai:copilot-workspace-project-root";

/** Sidebar group menu aliases (each backs a sectionSidebarApp, mirroring the AI section's menus). */
export const UAI_PINNED_MENU_ALIAS = "Uai.CopilotWorkspace.Menu.Pinned";
export const UAI_PROJECTS_MENU_ALIAS = "Uai.CopilotWorkspace.Menu.Projects";
export const UAI_RECENT_MENU_ALIAS = "Uai.CopilotWorkspace.Menu.Recent";

/** Workspace alias for the project entity workspace (matches its manifests' condition + editor). */
export const UAI_PROJECT_WORKSPACE_ALIAS = "Uai.CopilotWorkspace.Workspace.Project";
