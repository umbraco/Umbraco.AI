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
        alias: "UmbracoAiAgent.Workspace.Agent.View.Details",
        name: "Agent Details Workspace View",
        js: () => import("./views/agent-details-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Details",
            pathname: "details",
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
        type: "workspaceAction",
        kind: "default",
        alias: "UmbracoAiAgent.WorkspaceAction.Agent.Save",
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
