import { UAI_CONTEXT_ROOT_WORKSPACE_ALIAS, UAI_CONTEXT_ROOT_ENTITY_TYPE, UAI_CONTEXT_ICON } from "../../constants.js";
import { UAI_CONTEXT_COLLECTION_ALIAS } from "../../collection/constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_CONTEXT_ROOT_WORKSPACE_ALIAS,
        name: "Context Root Workspace",
        meta: {
            entityType: UAI_CONTEXT_ROOT_ENTITY_TYPE,
            headline: "Contexts",
        },
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "UmbracoAI.WorkspaceView.ContextRoot.Collection",
        name: "Context Root Collection Workspace View",
        meta: {
            label: "Collection",
            pathname: "collection",
            icon: UAI_CONTEXT_ICON,
            collectionAlias: UAI_CONTEXT_COLLECTION_ALIAS,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_CONTEXT_ROOT_WORKSPACE_ALIAS,
            },
        ],
    },
];
