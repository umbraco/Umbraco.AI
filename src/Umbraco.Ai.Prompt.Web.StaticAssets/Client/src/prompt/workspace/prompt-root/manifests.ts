import {
    UAI_PROMPT_ROOT_WORKSPACE_ALIAS,
    UAI_PROMPT_ROOT_ENTITY_TYPE,
    UAI_PROMPT_ICON,
} from "../../constants.js";
import { UAI_PROMPT_COLLECTION_ALIAS } from "../../collection/constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_PROMPT_ROOT_WORKSPACE_ALIAS,
        name: "Prompt Root Workspace",
        meta: {
            entityType: UAI_PROMPT_ROOT_ENTITY_TYPE,
            headline: "Prompts",
        },
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "UmbracoAiPrompt.WorkspaceView.PromptRoot.Collection",
        name: "Prompt Root Collection Workspace View",
        meta: {
            label: "Collection",
            pathname: "collection",
            icon: UAI_PROMPT_ICON,
            collectionAlias: UAI_PROMPT_COLLECTION_ALIAS,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_PROMPT_ROOT_WORKSPACE_ALIAS,
            },
        ],
    },
];
