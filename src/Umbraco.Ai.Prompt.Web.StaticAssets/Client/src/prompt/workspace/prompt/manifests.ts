import { UmbSubmitWorkspaceAction } from "@umbraco-cms/backoffice/workspace";
import { UAI_PROMPT_WORKSPACE_ALIAS, UAI_PROMPT_ENTITY_TYPE } from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "routable",
        alias: UAI_PROMPT_WORKSPACE_ALIAS,
        name: "Prompt Workspace",
        api: () => import("./prompt-workspace.context.js"),
        meta: {
            entityType: UAI_PROMPT_ENTITY_TYPE,
        },
    },
    {
        type: "workspaceView",
        alias: "UmbracoAiPrompt.Workspace.Prompt.View.Details",
        name: "Prompt Details Workspace View",
        js: () => import("./views/prompt-details-workspace-view.element.js"),
        weight: 100,
        meta: {
            label: "Details",
            pathname: "details",
            icon: "icon-settings",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_PROMPT_WORKSPACE_ALIAS,
            },
        ],
    },
    {
        type: "workspaceAction",
        kind: "default",
        alias: "UmbracoAiPrompt.WorkspaceAction.Prompt.Save",
        name: "Save Prompt",
        api: UmbSubmitWorkspaceAction,
        meta: {
            label: "Save",
            look: "primary",
            color: "positive",
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_PROMPT_WORKSPACE_ALIAS,
            },
        ],
    },
];
