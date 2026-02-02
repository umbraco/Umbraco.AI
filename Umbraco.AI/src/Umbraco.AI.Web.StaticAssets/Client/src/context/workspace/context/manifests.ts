import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UAI_CONTEXT_WORKSPACE_ALIAS, UAI_CONTEXT_ENTITY_TYPE } from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "routable",
        alias: UAI_CONTEXT_WORKSPACE_ALIAS,
        name: "Context Workspace",
        api: () => import("./context-workspace.context.js"),
        meta: {
            entityType: UAI_CONTEXT_ENTITY_TYPE,
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAI.Workspace.Context.View.Details",
        name: "Context Details Workspace View",
        js: () => import("./views/context-details-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Settings",
            pathname: "settings",
            icon: "icon-settings",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_CONTEXT_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceView",
        alias: "UmbracoAI.Workspace.Context.View.Info",
        name: "Context Info Workspace View",
        js: () => import("./views/context-info-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Info",
            pathname: "info",
            icon: "icon-info",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_CONTEXT_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceAction",
        kind: "default",
        alias: "UmbracoAI.WorkspaceAction.Context.Save",
        name: "Save Context",
        api: UmbSubmitWorkspaceAction,
        meta: {
            label: "Save",
            look: "primary",
            color: "positive",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_CONTEXT_WORKSPACE_ALIAS,
            },
        ],
    },
];
