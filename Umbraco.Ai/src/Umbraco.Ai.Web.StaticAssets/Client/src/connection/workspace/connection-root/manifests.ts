import {
    UAI_CONNECTION_ROOT_WORKSPACE_ALIAS,
    UAI_CONNECTION_ROOT_ENTITY_TYPE,
    UAI_CONNECTION_ICON,
    UAI_CONNECTION_COLLECTION_ALIAS,
} from "../../constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_CONNECTION_ROOT_WORKSPACE_ALIAS,
        name: "Connection Root Workspace",
        meta: {
            entityType: UAI_CONNECTION_ROOT_ENTITY_TYPE,
            headline: "Connections",
        },
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "UmbracoAi.WorkspaceView.ConnectionRoot.Collection",
        name: "Connection Root Collection Workspace View",
        meta: {
            label: "Collection",
            pathname: "collection",
            icon: UAI_CONNECTION_ICON,
            collectionAlias: UAI_CONNECTION_COLLECTION_ALIAS,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_CONNECTION_ROOT_WORKSPACE_ALIAS,
            },
        ],
    },
];
