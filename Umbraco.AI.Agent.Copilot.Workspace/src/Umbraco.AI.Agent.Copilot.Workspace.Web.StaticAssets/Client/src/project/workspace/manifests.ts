import { UmbSubmitWorkspaceAction, UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";
import { UAI_PROJECT_WORKSPACE_ALIAS, UAI_PROJECT_ENTITY_TYPE } from "../../constants.js";

const workspaceCondition = { alias: UMB_WORKSPACE_CONDITION_ALIAS, match: UAI_PROJECT_WORKSPACE_ALIAS };

/**
 * Project workspace extensions: the Details/Info tab views, the Save action, and the ⋯-menu entity
 * actions (New chat / Delete). The workspace context itself is provided by the editor element (the
 * section shell routes to it), so there is no routable `workspace` manifest — these are resolved by
 * `<umb-workspace-editor alias="…">` and the workspace condition against the provided context.
 */
export const projectWorkspaceManifests: UmbExtensionManifest[] = [
    {
        type: "workspaceView",
        alias: "Uai.CopilotWorkspace.Workspace.Project.View.Details",
        name: "Project Details Workspace View",
        js: () => import("./views/project-details-workspace-view.element.js"),
        weight: 200,
        meta: { label: "#uaiCopilotWorkspace_projectDetailsHeadline", pathname: "details", icon: "icon-settings" },
        conditions: [workspaceCondition],
    },
    {
        type: "workspaceView",
        alias: "Uai.CopilotWorkspace.Workspace.Project.View.Info",
        name: "Project Info Workspace View",
        js: () => import("./views/project-info-workspace-view.element.js"),
        weight: 100,
        meta: { label: "#uaiCopilotWorkspace_projectInfoHeadline", pathname: "info", icon: "icon-info" },
        conditions: [workspaceCondition],
    },
    {
        type: "workspaceAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.WorkspaceAction.Project.Save",
        name: "Save Project Workspace Action",
        api: UmbSubmitWorkspaceAction,
        meta: { label: "#buttons_save", look: "primary", color: "positive" },
        conditions: [workspaceCondition],
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Project.NewChat",
        name: "New Chat In Project Entity Action",
        weight: 200,
        api: () => import("../entity-actions/project-new-chat.action.js"),
        forEntityTypes: [UAI_PROJECT_ENTITY_TYPE],
        meta: { icon: "icon-add", label: "#uaiCopilotWorkspace_projectNewChat" },
    },
    {
        type: "entityAction",
        kind: "default",
        alias: "Uai.CopilotWorkspace.EntityAction.Project.Delete",
        name: "Delete Project Entity Action",
        weight: 100,
        api: () => import("../entity-actions/project-delete.action.js"),
        forEntityTypes: [UAI_PROJECT_ENTITY_TYPE],
        meta: { icon: "icon-trash", label: "#actions_delete" },
    },
];
