import {
    UAI_TEST_COLLECTION_ALIAS,
    UAI_TEST_COLLECTION_REPOSITORY_ALIAS,
    UAI_TEST_RUN_COLLECTION_ALIAS,
    UAI_TEST_RUN_COLLECTION_REPOSITORY_ALIAS,
    UAI_TEST_RUN_ENTITY_TYPE,
} from "../constants.js";
import { UMB_COLLECTION_ALIAS_CONDITION } from "@umbraco-cms/backoffice/collection";
import { testCollectionActionManifests } from "./action/manifests.js";
import { testBulkActionManifests } from "./bulk-action/manifests.js";

export const testCollectionManifests: Array<UmbExtensionManifest> = [
    // Tests collection
    {
        type: "collection",
        kind: "default",
        alias: UAI_TEST_COLLECTION_ALIAS,
        name: "Test Collection",
        element: () => import("./test-collection.element.js"),
        meta: {
            repositoryAlias: UAI_TEST_COLLECTION_REPOSITORY_ALIAS,
        },
    },
    {
        type: "collectionView",
        alias: "UmbracoAI.CollectionView.Test.Table",
        name: "Test Table View",
        element: () => import("./views/table/test-table-collection-view.element.js"),
        meta: {
            label: "Table",
            icon: "icon-list",
            pathName: "table",
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_TEST_COLLECTION_ALIAS }],
    },
    ...testCollectionActionManifests,
    ...testBulkActionManifests,

    // Test Runs collection
    {
        type: "collection",
        kind: "default",
        alias: UAI_TEST_RUN_COLLECTION_ALIAS,
        name: "Test Run Collection",
        element: () => import("./test-run-collection.element.js"),
        meta: {
            repositoryAlias: UAI_TEST_RUN_COLLECTION_REPOSITORY_ALIAS,
        },
    },
    {
        type: "collectionView",
        alias: "UmbracoAI.CollectionView.TestRun.Table",
        name: "Test Run Table View",
        element: () => import("./views/table/test-run-table-collection-view.element.js"),
        meta: {
            label: "Table",
            icon: "icon-list",
            pathName: "table",
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_TEST_RUN_COLLECTION_ALIAS }],
    },
    {
        type: "entityBulkAction",
        kind: "default",
        alias: "UmbracoAI.EntityBulkAction.TestRun.Delete",
        name: "Delete Test Runs Bulk Action",
        weight: 100,
        api: () => import("./bulk-action/test-run-bulk-delete.action.js"),
        forEntityTypes: [UAI_TEST_RUN_ENTITY_TYPE],
        meta: {
            icon: "icon-trash",
            label: "#actions_delete",
        },
        conditions: [
            {
                alias: UMB_COLLECTION_ALIAS_CONDITION,
                match: UAI_TEST_RUN_COLLECTION_ALIAS,
            },
        ],
    },
];
