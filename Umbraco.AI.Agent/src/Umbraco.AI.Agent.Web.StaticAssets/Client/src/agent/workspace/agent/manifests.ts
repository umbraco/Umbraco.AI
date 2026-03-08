import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UAI_AGENT_WORKSPACE_ALIAS, UAI_AGENT_ENTITY_TYPE } from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";
import { UAI_AGENT_TYPE_CONDITION_ALIAS, type UaiAgentTypeConditionConfig } from "./agent-type.condition.js";

export const manifests: Array<UmbExtensionManifest> = [
    // Workspace condition: matches agent type from workspace context
    {
        type: "condition",
        alias: UAI_AGENT_TYPE_CONDITION_ALIAS,
        name: "Agent Type Condition",
        api: () => import("./agent-type.condition.js"),
    },
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
    // Settings tab (all agent types)
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
    // Workflow tab (orchestrated agents only)
    {
        type: "workspaceView",
        alias: "UmbracoAIAgent.Workspace.Agent.View.Workflow",
        name: "Agent Workflow Workspace View",
        js: () => import("./views/agent-workflow-workspace-view.element.js"),
        weight: 290,
        meta: {
            label: "Workflow",
            pathname: "workflow",
            icon: "icon-mindmap",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_AGENT_WORKSPACE_ALIAS,
            },
            {
                alias: UAI_AGENT_TYPE_CONDITION_ALIAS,
                match: "orchestrated",
            } as UaiAgentTypeConditionConfig,
        ],
    },
    // Availability tab (all agent types)
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
    // Permissions tab (standard agents only)
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
            {
                alias: UAI_AGENT_TYPE_CONDITION_ALIAS,
                match: "standard",
            } as UaiAgentTypeConditionConfig,
        ],
    },
    // Info tab (all agent types)
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
