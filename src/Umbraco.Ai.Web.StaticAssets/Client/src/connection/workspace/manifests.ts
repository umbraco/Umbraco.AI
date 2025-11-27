import type { ManifestWorkspace, ManifestWorkspaceView, ManifestWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UaiConnectionConstants } from "../constants.js";

export const connectionWorkspaceManifests: Array<ManifestWorkspace | ManifestWorkspaceView | ManifestWorkspaceAction> = [
    {
        type: "workspace",
        kind: "routable",
        alias: UaiConnectionConstants.Workspace.Root,
        name: "Connection Root Workspace",
        api: () => import("./connection-root-workspace.context.js"),
        element: () => import("./connection-root-workspace.element.js"),
        meta: {
            entityType: UaiConnectionConstants.EntityType.Root,
        },
    },
    {
        type: "workspace",
        kind: "routable",
        alias: UaiConnectionConstants.Workspace.Entity,
        name: "Connection Workspace",
        api: () => import("./connection-workspace.context.js"),
        element: () => import("./connection-workspace-editor.element.js"),
        meta: {
            entityType: UaiConnectionConstants.EntityType.Entity,
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAi.Workspace.Connection.View.Details",
        name: "Connection Details Workspace View",
        element: () => import("../views/connection-details.element.js"),
        weight: 100,
        meta: {
            label: "Details",
            pathname: "details",
            icon: "icon-settings",
        },
        conditions: [
            {
                alias: "Umb.Condition.WorkspaceAlias",
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
                alias: "Umb.Condition.WorkspaceAlias",
                match: UaiConnectionConstants.Workspace.Entity,
            },
        ],
    },
];
