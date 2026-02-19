import { UAI_TEST_COLLECTION_ALIAS, UAI_TEST_COLLECTION_REPOSITORY_ALIAS } from "../constants.js";

export const testCollectionManifests: Array<UmbExtensionManifest> = [
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
];
