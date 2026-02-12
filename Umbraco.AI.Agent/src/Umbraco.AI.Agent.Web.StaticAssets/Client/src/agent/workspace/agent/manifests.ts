import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UAI_AGENT_WORKSPACE_ALIAS, UAI_AGENT_ENTITY_TYPE } from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "routable",
        alias: UAI_AGENT_WORKSPACE_ALIAS,
        name: "Agent Workspace",
        api: () => import("./agent-workspace.context.js"),
        meta: {
            entityType: UAI_AGENT_ENTITY_TYPE,
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAIAgent.Workspace.Agent.View.Details",
        name: "Agent Details Workspace View",
        js: () => import("./views/agent-details-workspace-view.element.js"),
        weight: 300,
        meta: {
            label: "Settings",
            pathname: "settings",
            icon: "icon-settings",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_AGENT_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceView",
        alias: "UmbracoAIAgent.Workspace.Agent.View.Availability",
        name: "Agent Availability Workspace View",
        js: () => import("./views/agent-availability-workspace-view.element.js"),
        weight: 250,
        meta: {
            label: "Availability",
            pathname: "availability",
            icon: "icon-locate",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_AGENT_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceView",
        alias: "UmbracoAIAgent.Workspace.Agent.View.Permissions",
        name: "Agent Permissions Workspace View",
        js: () => import("./views/agent-permissions-workspace-view.element.js"),
        weight: 200,
        meta: {
            label: "Permissions",
            pathname: "permissions",
            icon: "icon-lock",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_AGENT_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceView",
        alias: "UmbracoAIAgent.Workspace.Agent.View.Info",
        name: "Agent Info Workspace View",
        js: () => import("./views/agent-info-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Info",
            pathname: "info",
            icon: "icon-info",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_AGENT_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceAction",
        kind: "default",
        alias: "UmbracoAIAgent.WorkspaceAction.Agent.Save",
        name: "Save Agent",
        api: UmbSubmitWorkspaceAction,
        meta: {
            label: "Save",
            look: "primary",
            color: "positive",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_AGENT_WORKSPACE_ALIAS,
            },
        ],
    },
];
