import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UAI_GUARDRAIL_WORKSPACE_ALIAS, UAI_GUARDRAIL_ENTITY_TYPE } from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "routable",
        alias: UAI_GUARDRAIL_WORKSPACE_ALIAS,
        name: "Guardrail Workspace",
        api: () => import("./guardrail-workspace.context.js"),
        meta: {
            entityType: UAI_GUARDRAIL_ENTITY_TYPE,
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAI.Workspace.Guardrail.View.Details",
        name: "Guardrail Details Workspace View",
        js: () => import("./views/guardrail-details-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Settings",
            pathname: "settings",
            icon: "icon-settings",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_GUARDRAIL_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceView",
        alias: "UmbracoAI.Workspace.Guardrail.View.Info",
        name: "Guardrail Info Workspace View",
        js: () => import("./views/guardrail-info-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Info",
            pathname: "info",
            icon: "icon-info",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_GUARDRAIL_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceAction",
        kind: "default",
        alias: "UmbracoAI.WorkspaceAction.Guardrail.Save",
        name: "Save Guardrail",
        api: UmbSubmitWorkspaceAction,
        meta: {
            label: "Save",
            look: "primary",
            color: "positive",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_GUARDRAIL_WORKSPACE_ALIAS,
            },
        ],
    },
];
