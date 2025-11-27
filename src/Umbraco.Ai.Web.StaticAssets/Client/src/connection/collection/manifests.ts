import type { ManifestCollection, ManifestCollectionView, ManifestCollectionAction } from "@umbraco-cms/backoffice/collection";
import { UaiConnectionConstants } from "../constants.js";
import { connectionCollectionActionManifests } from "./action/manifests.js";

export const connectionCollectionManifests: Array<ManifestCollection | ManifestCollectionView | ManifestCollectionAction> = [
    {
        type: "collection",
        kind: "default",
        alias: UaiConnectionConstants.Collection,
        name: "Connection Collection",
        meta: {
            repositoryAlias: UaiConnectionConstants.Repository.Collection,
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
        conditions: [{ alias: "Umb.Condition.CollectionAlias", match: UaiConnectionConstants.Collection }],
    },
    ...connectionCollectionActionManifests,
];
