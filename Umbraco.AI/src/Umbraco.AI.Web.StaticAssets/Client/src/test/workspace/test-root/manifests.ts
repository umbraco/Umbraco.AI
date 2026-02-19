import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";
import {
    UAI_TEST_ROOT_WORKSPACE_ALIAS,
    UAI_TEST_ROOT_ENTITY_TYPE,
    UAI_TEST_ICON,
    UAI_TEST_COLLECTION_ALIAS,
} from "../../constants.js";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_TEST_ROOT_WORKSPACE_ALIAS,
        name: "Tests Root Workspace",
        meta: {
            entityType: UAI_TEST_ROOT_ENTITY_TYPE,
            headline: "Tests",
        },
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "UmbracoAI.WorkspaceView.TestRoot.Collection",
        name: "Test Root Collection Workspace View",
        meta: {
            label: "Collection",
            pathname: "collection",
            icon: UAI_TEST_ICON,
            collectionAlias: UAI_TEST_COLLECTION_ALIAS,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_TEST_ROOT_WORKSPACE_ALIAS,
            },
        ],
    },
];
