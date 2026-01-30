import { UAI_CONTEXT_COLLECTION_ALIAS } from "./constants.js";
import { UAI_CONTEXT_COLLECTION_REPOSITORY_ALIAS } from "../repository/constants.js";
import { contextCollectionActionManifests } from "./action/manifests.js";
import { contextBulkActionManifests } from "./bulk-action/manifests.js";

export const contextCollectionManifests: Array<UmbExtensionManifest> = [
    {
        type: "collection",
        kind: "default",
        alias: UAI_CONTEXT_COLLECTION_ALIAS,
        name: "Context Collection",
        element: () => import("./context-collection.element.js"),
        meta: {
            repositoryAlias: UAI_CONTEXT_COLLECTION_REPOSITORY_ALIAS,
        },
    },
    {
        type: "collectionView",
        alias: "UmbracoAi.CollectionView.Context.Table",
        name: "Context Table View",
        element: () => import("./views/table/context-table-collection-view.element.js"),
        meta: {
            label: "Table",
            icon: "icon-list",
            pathName: "table",
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_CONTEXT_COLLECTION_ALIAS }],
    },
    ...contextCollectionActionManifests,
    ...contextBulkActionManifests,
];
