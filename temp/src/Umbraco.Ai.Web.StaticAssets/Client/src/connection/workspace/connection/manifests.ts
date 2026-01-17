import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UAI_CONNECTION_WORKSPACE_ALIAS, UAI_CONNECTION_ENTITY_TYPE } from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "routable",
        alias: UAI_CONNECTION_WORKSPACE_ALIAS,
        name: "Connection Workspace",
        api: () => import("./connection-workspace.context.js"),
        meta: {
            entityType: UAI_CONNECTION_ENTITY_TYPE,
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAi.Workspace.Connection.View.Details",
        name: "Connection Details Workspace View",
        js: () => import("./views/connection-details-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Details",
            pathname: "details",
            icon: "icon-settings",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_CONNECTION_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceAction",
        kind: "default",
        alias: "UmbracoAi.WorkspaceAction.Connection.Save",
        name: "Save Connection",
        api: UmbSubmitWorkspaceAction,
        meta: {
            label: "Save",
            look: "primary",
            color: "positive",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_CONNECTION_WORKSPACE_ALIAS,
            },
        ],
    },
];
