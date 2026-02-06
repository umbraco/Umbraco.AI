import { UAI_AGENT_ROOT_WORKSPACE_ALIAS, UAI_AGENT_ROOT_ENTITY_TYPE, UAI_AGENT_ICON } from "../../constants.js";
import { UAI_AGENT_COLLECTION_ALIAS } from "../../collection/constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_AGENT_ROOT_WORKSPACE_ALIAS,
        name: "Agent Root Workspace",
        meta: {
            entityType: UAI_AGENT_ROOT_ENTITY_TYPE,
            headline: "Agents",
        },
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "UmbracoAIAgent.WorkspaceView.AgentRoot.Collection",
        name: "Agent Root Collection Workspace View",
        meta: {
            label: "Collection",
            pathname: "collection",
            icon: UAI_AGENT_ICON,
            collectionAlias: UAI_AGENT_COLLECTION_ALIAS,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_AGENT_ROOT_WORKSPACE_ALIAS,
            },
        ],
    },
];
