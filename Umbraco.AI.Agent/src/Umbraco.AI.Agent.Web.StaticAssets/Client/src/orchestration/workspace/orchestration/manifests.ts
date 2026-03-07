import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UAI_ORCHESTRATION_WORKSPACE_ALIAS, UAI_ORCHESTRATION_ENTITY_TYPE } from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "routable",
        alias: UAI_ORCHESTRATION_WORKSPACE_ALIAS,
        name: "Orchestration Workspace",
        api: () => import("./orchestration-workspace.context.js"),
        meta: {
            entityType: UAI_ORCHESTRATION_ENTITY_TYPE,
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAIAgent.Workspace.Orchestration.View.Details",
        name: "Orchestration Details Workspace View",
        js: () => import("./views/orchestration-details-workspace-view.element.js"),
        weight: 300,
        meta: {
            label: "Settings",
            pathname: "settings",
            icon: "icon-settings",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_ORCHESTRATION_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceView",
        alias: "UmbracoAIAgent.Workspace.Orchestration.View.Availability",
        name: "Orchestration Availability Workspace View",
        js: () => import("./views/orchestration-availability-workspace-view.element.js"),
        weight: 250,
        meta: {
            label: "Availability",
            pathname: "availability",
            icon: "icon-locate",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_ORCHESTRATION_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceView",
        alias: "UmbracoAIAgent.Workspace.Orchestration.View.Info",
        name: "Orchestration Info Workspace View",
        js: () => import("./views/orchestration-info-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Info",
            pathname: "info",
            icon: "icon-info",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_ORCHESTRATION_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceAction",
        kind: "default",
        alias: "UmbracoAIAgent.WorkspaceAction.Orchestration.Save",
        name: "Save Orchestration",
        api: UmbSubmitWorkspaceAction,
        meta: {
            label: "Save",
            look: "primary",
            color: "positive",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_ORCHESTRATION_WORKSPACE_ALIAS,
            },
        ],
    },
];
