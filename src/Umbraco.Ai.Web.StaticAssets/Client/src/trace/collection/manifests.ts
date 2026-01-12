import { UAI_TRACE_COLLECTION_ALIAS, UAI_TRACE_COLLECTION_REPOSITORY_ALIAS, UAI_TRACE_TABLE_VIEW_ALIAS } from "../constants.js";

export const traceCollectionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collection",
        kind: "default",
        alias: UAI_TRACE_COLLECTION_ALIAS,
        name: "Trace Collection",
        element: () => import("./trace-collection.element.js"),
        meta: {
            repositoryAlias: UAI_TRACE_COLLECTION_REPOSITORY_ALIAS,
        },
    },
    {
        type: "collectionView",
        alias: UAI_TRACE_TABLE_VIEW_ALIAS,
        name: "Trace Table View",
        element: () => import("./views/table/trace-table-collection-view.element.js"),
        meta: {
            label: "Table",
            icon: "icon-list",
            pathName: "table",
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_TRACE_COLLECTION_ALIAS }],
    },
];
