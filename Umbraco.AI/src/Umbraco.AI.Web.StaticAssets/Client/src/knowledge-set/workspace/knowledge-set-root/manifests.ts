import { UAI_KNOWLEDGE_SET_ROOT_WORKSPACE_ALIAS, UAI_KNOWLEDGE_SET_ROOT_ENTITY_TYPE, UAI_KNOWLEDGE_SET_ICON } from "../../constants.js";
import { UAI_KNOWLEDGE_SET_COLLECTION_ALIAS } from "../../collection/constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

/**
 * Read-only root workspace for the Knowledge Sets section: a collection view only, no entity actions.
 */
export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_KNOWLEDGE_SET_ROOT_WORKSPACE_ALIAS,
        name: "Knowledge Set Root Workspace",
        meta: {
            entityType: UAI_KNOWLEDGE_SET_ROOT_ENTITY_TYPE,
            headline: "Knowledge Sets",
        },
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "UmbracoAI.WorkspaceView.KnowledgeSetRoot.Collection",
        name: "Knowledge Set Root Collection Workspace View",
        meta: {
            label: "Collection",
            pathname: "collection",
            icon: UAI_KNOWLEDGE_SET_ICON,
            collectionAlias: UAI_KNOWLEDGE_SET_COLLECTION_ALIAS,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_KNOWLEDGE_SET_ROOT_WORKSPACE_ALIAS,
            },
        ],
    },
];
