import {
    UAI_ORCHESTRATION_ROOT_WORKSPACE_ALIAS,
    UAI_ORCHESTRATION_ROOT_ENTITY_TYPE,
    UAI_ORCHESTRATION_ICON,
} from "../../constants.js";
import { UAI_ORCHESTRATION_COLLECTION_ALIAS } from "../../collection/constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_ORCHESTRATION_ROOT_WORKSPACE_ALIAS,
        name: "Orchestration Root Workspace",
        meta: {
            entityType: UAI_ORCHESTRATION_ROOT_ENTITY_TYPE,
            headline: "Orchestrations",
        },
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "UmbracoAIAgent.WorkspaceView.OrchestrationRoot.Collection",
        name: "Orchestration Root Collection Workspace View",
        meta: {
            label: "Collection",
            pathname: "collection",
            icon: UAI_ORCHESTRATION_ICON,
            collectionAlias: UAI_ORCHESTRATION_COLLECTION_ALIAS,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_ORCHESTRATION_ROOT_WORKSPACE_ALIAS,
            },
        ],
    },
];
