import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UaiConnectionConstants } from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "routable",
        alias: UaiConnectionConstants.Workspace.Entity,
        name: "Connection Workspace",
        api: () => import("./connection-workspace.context.js"),
        meta: {
            entityType: UaiConnectionConstants.EntityType.Entity,
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
                match: UaiConnectionConstants.Workspace.Entity,
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
                match: UaiConnectionConstants.Workspace.Entity,
            },
        ],
    },
];
