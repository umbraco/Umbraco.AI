import type { ManifestCollection, ManifestCollectionView, ManifestCollectionAction } from "@umbraco-cms/backoffice/collection";
import { UAI_CONNECTION_COLLECTION_ALIAS } from "./constants.js";
import { UAI_CONNECTION_COLLECTION_REPOSITORY_ALIAS } from "../repository/constants.js";
import { connectionCollectionActionManifests } from "./action/manifests.js";

export const connectionCollectionManifests: Array<ManifestCollection | ManifestCollectionView | ManifestCollectionAction> = [
    {
        type: "collection",
        kind: "default",
        alias: UAI_CONNECTION_COLLECTION_ALIAS,
        name: "Connection Collection",
        meta: {
            repositoryAlias: UAI_CONNECTION_COLLECTION_REPOSITORY_ALIAS,
        },
    },
    {
        type: "collectionView",
        alias: "UmbracoAi.CollectionView.Connection.Table",
        name: "Connection Table View",
        element: () => import("./views/table/connection-table-collection-view.element.js"),
        meta: {
            label: "Table",
            icon: "icon-list",
            pathName: "table",
        },
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UAI_CONNECTION_COLLECTION_ALIAS }],
    },
    ...connectionCollectionActionManifests,
];
